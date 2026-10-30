using System;
using System.Collections.Generic;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.S07
{
    public enum WorkspaceApp
    {
        Unknown = 0,
        Home = 1,
        Science3D = 2,
        Settings = 3,
        AgentActivity = 4,
        Document = 5,
    }

    public enum WorkspaceLayoutError
    {
        None = 0,
        EmptyPayload,
        InvalidJson,
        UnsupportedVersion,
        InvalidSelectedApp,
        InvalidPanel,
        DuplicatePanelId,
        InvalidFocus,
        RestoreRejected,
    }

    [Serializable]
    public sealed class WorkspacePanelData
    {
        public string panelId;
        public int kind;
        public int visibility;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float width;
        public float height;
        public int zOrder;
        public bool isFocused;
        public bool isPinned;

        public WorkspacePanelData Clone()
        {
            return (WorkspacePanelData)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class WorkspaceLayoutData
    {
        public int schemaVersion;
        public string selectedApp;
        public long savedUnixMilliseconds;
        public WorkspacePanelData[] panels;

        public WorkspaceLayoutData Clone()
        {
            WorkspacePanelData[] cloned = panels == null
                ? null
                : Array.ConvertAll(panels, panel => panel?.Clone());
            return new WorkspaceLayoutData
            {
                schemaVersion = schemaVersion,
                selectedApp = selectedApp,
                savedUnixMilliseconds = savedUnixMilliseconds,
                panels = cloned,
            };
        }
    }

    [Serializable]
    internal sealed class WorkspaceSchemaHeader
    {
        public int schemaVersion;
    }

    [Serializable]
    internal sealed class WorkspaceLayoutV1
    {
        public int schemaVersion;
        public string selectedApp;
        public long savedUnixMilliseconds;
        public WorkspacePanelV1[] panels;
    }

    [Serializable]
    internal sealed class WorkspacePanelV1
    {
        public string panelId;
        public int kind;
        public int visibility;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float width;
        public float height;
        public int zOrder;
        public bool isFocused;
    }

    public static class WorkspaceLayoutCodec
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumJsonCharacters = 64 * 1024;

        public static WorkspaceLayoutData Capture(
            PanelSnapshot snapshot,
            WorkspaceApp selectedApp,
            long savedUnixMilliseconds
        )
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            WorkspacePanelData[] panels = new WorkspacePanelData[snapshot.Panels.Count];
            for (int index = 0; index < snapshot.Panels.Count; index += 1)
            {
                PanelState state = snapshot.Panels[index];
                panels[index] = new WorkspacePanelData
                {
                    panelId = state.PanelId,
                    kind = (int)state.Kind,
                    visibility = (int)state.Visibility,
                    positionX = state.Bounds.PositionMeters.x,
                    positionY = state.Bounds.PositionMeters.y,
                    positionZ = state.Bounds.PositionMeters.z,
                    width = state.Bounds.SizeMeters.x,
                    height = state.Bounds.SizeMeters.y,
                    zOrder = state.ZOrder,
                    isFocused = state.IsFocused,
                    isPinned = state.IsPinned,
                };
            }

            return new WorkspaceLayoutData
            {
                schemaVersion = CurrentSchemaVersion,
                selectedApp = selectedApp.ToString(),
                savedUnixMilliseconds = savedUnixMilliseconds,
                panels = panels,
            };
        }

        public static bool TrySerialize(
            WorkspaceLayoutData data,
            out string json,
            out WorkspaceLayoutError error
        )
        {
            json = null;
            if (!TryValidate(data, out error))
            {
                return false;
            }

            json = JsonUtility.ToJson(data, false);
            if (string.IsNullOrEmpty(json) || json.Length > MaximumJsonCharacters)
            {
                json = null;
                error = WorkspaceLayoutError.InvalidJson;
                return false;
            }

            return true;
        }

        public static bool TryDeserialize(
            string json,
            out WorkspaceLayoutData data,
            out WorkspaceLayoutError error
        )
        {
            data = null;
            error = WorkspaceLayoutError.None;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = WorkspaceLayoutError.EmptyPayload;
                return false;
            }

            if (json.Length > MaximumJsonCharacters)
            {
                error = WorkspaceLayoutError.InvalidJson;
                return false;
            }

            try
            {
                WorkspaceSchemaHeader header = JsonUtility.FromJson<WorkspaceSchemaHeader>(json);
                if (header == null)
                {
                    error = WorkspaceLayoutError.InvalidJson;
                    return false;
                }

                switch (header.schemaVersion)
                {
                    case 1:
                        WorkspaceLayoutV1 legacy = JsonUtility.FromJson<WorkspaceLayoutV1>(json);
                        data = MigrateV1(legacy);
                        break;
                    case CurrentSchemaVersion:
                        data = JsonUtility.FromJson<WorkspaceLayoutData>(json);
                        break;
                    default:
                        error = WorkspaceLayoutError.UnsupportedVersion;
                        return false;
                }
            }
            catch (ArgumentException)
            {
                error = WorkspaceLayoutError.InvalidJson;
                return false;
            }

            if (!TryValidate(data, out error))
            {
                data = null;
                return false;
            }

            return true;
        }

        public static bool TryValidate(
            WorkspaceLayoutData data,
            out WorkspaceLayoutError error
        )
        {
            error = WorkspaceLayoutError.None;
            if (data == null || data.panels == null)
            {
                error = WorkspaceLayoutError.InvalidJson;
                return false;
            }

            if (data.schemaVersion != CurrentSchemaVersion)
            {
                error = WorkspaceLayoutError.UnsupportedVersion;
                return false;
            }

            if (
                !Enum.TryParse(data.selectedApp, false, out WorkspaceApp selectedApp)
                || selectedApp == WorkspaceApp.Unknown
                || !Enum.IsDefined(typeof(WorkspaceApp), selectedApp)
            )
            {
                error = WorkspaceLayoutError.InvalidSelectedApp;
                return false;
            }

            if (data.panels.Length == 0 || data.panels.Length > PanelRegistry.MaximumPanels)
            {
                error = WorkspaceLayoutError.InvalidPanel;
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int focusedCount = 0;
            foreach (WorkspacePanelData panel in data.panels)
            {
                if (panel == null || !ids.Add(panel.panelId))
                {
                    error = panel == null
                        ? WorkspaceLayoutError.InvalidPanel
                        : WorkspaceLayoutError.DuplicatePanelId;
                    return false;
                }

                PanelKind kind = (PanelKind)panel.kind;
                PanelVisibility visibility = (PanelVisibility)panel.visibility;
                PanelBounds bounds = new PanelBounds(
                    new Vector3(panel.positionX, panel.positionY, panel.positionZ),
                    new Vector2(panel.width, panel.height)
                );
                if (
                    !PanelDefinition.IsValidPanelId(panel.panelId)
                    || kind == PanelKind.Unknown
                    || !Enum.IsDefined(typeof(PanelKind), kind)
                    || !Enum.IsDefined(typeof(PanelVisibility), visibility)
                    || !bounds.IsValid()
                    || panel.zOrder < 0
                    || panel.zOrder > 1_000_000
                )
                {
                    error = WorkspaceLayoutError.InvalidPanel;
                    return false;
                }

                if (panel.isFocused)
                {
                    focusedCount += 1;
                    if (visibility != PanelVisibility.Visible)
                    {
                        error = WorkspaceLayoutError.InvalidFocus;
                        return false;
                    }
                }

                if (visibility == PanelVisibility.Closed && panel.zOrder != 0)
                {
                    error = WorkspaceLayoutError.InvalidPanel;
                    return false;
                }
            }

            if (focusedCount > 1)
            {
                error = WorkspaceLayoutError.InvalidFocus;
                return false;
            }

            return true;
        }

        public static bool TryRestoreRegistry(
            WorkspaceLayoutData data,
            out PanelRegistry registry,
            out WorkspaceLayoutError error
        )
        {
            registry = null;
            if (!TryValidate(data, out error))
            {
                return false;
            }

            PanelRegistry candidate = new PanelRegistry();
            List<WorkspacePanelData> active = new List<WorkspacePanelData>();
            foreach (WorkspacePanelData panel in data.panels)
            {
                PanelBounds bounds = new PanelBounds(
                    new Vector3(panel.positionX, panel.positionY, panel.positionZ),
                    new Vector2(panel.width, panel.height)
                );
                if (
                    !PanelDefinition.TryCreate(
                        panel.panelId,
                        (PanelKind)panel.kind,
                        bounds,
                        out PanelDefinition definition
                    )
                    || !Apply(candidate, PanelCommand.Register(candidate.Version, definition))
                )
                {
                    error = WorkspaceLayoutError.RestoreRejected;
                    return false;
                }

                if ((PanelVisibility)panel.visibility != PanelVisibility.Closed)
                {
                    active.Add(panel);
                }
            }

            active.Sort(CompareRestoreOrder);
            foreach (WorkspacePanelData panel in active)
            {
                if (
                    !Apply(
                        candidate,
                        PanelCommand.For(
                            PanelCommandKind.Open,
                            candidate.Version,
                            panel.panelId
                        )
                    )
                )
                {
                    error = WorkspaceLayoutError.RestoreRejected;
                    return false;
                }

                if ((PanelVisibility)panel.visibility == PanelVisibility.Minimized)
                {
                    if (
                        !Apply(
                            candidate,
                            PanelCommand.For(
                                PanelCommandKind.Minimize,
                                candidate.Version,
                                panel.panelId
                            )
                        )
                    )
                    {
                        error = WorkspaceLayoutError.RestoreRejected;
                        return false;
                    }
                }

                if (panel.isPinned)
                {
                    if (
                        !Apply(
                            candidate,
                            PanelCommand.For(
                                PanelCommandKind.SetPinned,
                                candidate.Version,
                                panel.panelId,
                                true
                            )
                        )
                    )
                    {
                        error = WorkspaceLayoutError.RestoreRejected;
                        return false;
                    }
                }
            }

            WorkspacePanelData focused = Array.Find(data.panels, panel => panel.isFocused);
            if (
                focused != null
                && candidate.TryGet(focused.panelId, out PanelState state)
                && !state.IsFocused
                && !Apply(
                    candidate,
                    PanelCommand.For(
                        PanelCommandKind.Focus,
                        candidate.Version,
                        focused.panelId
                    )
                )
            )
            {
                error = WorkspaceLayoutError.RestoreRejected;
                return false;
            }

            registry = candidate;
            error = WorkspaceLayoutError.None;
            return true;
        }

        private static WorkspaceLayoutData MigrateV1(WorkspaceLayoutV1 legacy)
        {
            if (legacy == null)
            {
                return null;
            }

            WorkspacePanelData[] panels = legacy.panels == null
                ? null
                : Array.ConvertAll(
                    legacy.panels,
                    panel =>
                        panel == null
                            ? null
                            : new WorkspacePanelData
                            {
                                panelId = panel.panelId,
                                kind = panel.kind,
                                visibility = panel.visibility,
                                positionX = panel.positionX,
                                positionY = panel.positionY,
                                positionZ = panel.positionZ,
                                width = panel.width,
                                height = panel.height,
                                zOrder = panel.zOrder,
                                isFocused = panel.isFocused,
                                isPinned = false,
                            }
                );
            return new WorkspaceLayoutData
            {
                schemaVersion = CurrentSchemaVersion,
                selectedApp = string.IsNullOrEmpty(legacy.selectedApp)
                    ? WorkspaceApp.Home.ToString()
                    : legacy.selectedApp,
                savedUnixMilliseconds = legacy.savedUnixMilliseconds,
                panels = panels,
            };
        }

        private static int CompareRestoreOrder(
            WorkspacePanelData left,
            WorkspacePanelData right
        )
        {
            int focus = left.isFocused.CompareTo(right.isFocused);
            if (focus != 0)
            {
                return focus;
            }

            int zOrder = left.zOrder.CompareTo(right.zOrder);
            return zOrder != 0
                ? zOrder
                : string.CompareOrdinal(left.panelId, right.panelId);
        }

        private static bool Apply(PanelRegistry registry, PanelCommand command)
        {
            return registry.Apply(command).Accepted;
        }
    }
}
