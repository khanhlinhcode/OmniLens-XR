using System.Collections.Generic;
using NUnit.Framework;
using OmniLensXR.S05;
using UnityEngine;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S05PanelLifecycleTests
    {
        [Test]
        public void DefinitionRejectsInvalidIdKindAndBounds()
        {
            PanelBounds valid = Bounds(0.0f);
            Assert.That(
                PanelDefinition.TryCreate("bad id", PanelKind.Chat, valid, out _),
                Is.False
            );
            Assert.That(
                PanelDefinition.TryCreate("chat", PanelKind.Unknown, valid, out _),
                Is.False
            );
            Assert.That(
                PanelDefinition.TryCreate(
                    "chat",
                    PanelKind.Chat,
                    new PanelBounds(
                        new Vector3(float.NaN, 0.0f, 2.0f),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out _
                ),
                Is.False
            );
            Assert.That(
                PanelDefinition.TryCreate(
                    "chat",
                    PanelKind.Chat,
                    new PanelBounds(
                        new Vector3(0.0f, 0.0f, 0.5f),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out _
                ),
                Is.False
            );
            Assert.That(
                PanelDefinition.TryCreate(
                    "chat",
                    PanelKind.Chat,
                    new PanelBounds(
                        new Vector3(2.0f, 0.0f, 1.0f),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void DuplicateStaleUnknownAndLimitFailuresDoNotMutateVersion()
        {
            PanelRegistry registry = new PanelRegistry();
            PanelDefinition chat = Definition("chat", PanelKind.Chat, 0.0f);
            AssertAccepted(registry, PanelCommand.Register(0, chat));
            long stableVersion = registry.Version;

            AssertRejected(
                registry,
                PanelCommand.Register(stableVersion, chat),
                PanelApplyError.DuplicatePanelId,
                stableVersion
            );
            AssertRejected(
                registry,
                PanelCommand.For(PanelCommandKind.Open, 0, "chat"),
                PanelApplyError.StaleVersion,
                stableVersion
            );
            AssertRejected(
                registry,
                PanelCommand.For(
                    PanelCommandKind.Open,
                    stableVersion,
                    "missing"
                ),
                PanelApplyError.UnknownPanel,
                stableVersion
            );

            for (int index = 1; index < PanelRegistry.MaximumPanels; index += 1)
            {
                AssertAccepted(
                    registry,
                    PanelCommand.Register(
                        registry.Version,
                        Definition("panel_" + index, PanelKind.Notes, index * 0.1f)
                    )
                );
            }

            long fullVersion = registry.Version;
            AssertRejected(
                registry,
                PanelCommand.Register(
                    fullVersion,
                    Definition("overflow", PanelKind.Document, 0.0f)
                ),
                PanelApplyError.PanelLimitReached,
                fullVersion
            );
        }

        [Test]
        public void NineteenTransitionsPreserveFocusVisibilityAndSnapshot()
        {
            PanelRegistry registry = RegistryWithFourPanels();
            Assert.That(registry.Version, Is.EqualTo(4));

            Apply(registry, PanelCommandKind.Open, "chat");
            AssertFocus(registry, "chat");
            Apply(registry, PanelCommandKind.Open, "science-3d");
            AssertFocus(registry, "science-3d");
            Apply(registry, PanelCommandKind.Open, "document");
            AssertFocus(registry, "document");
            Apply(registry, PanelCommandKind.Focus, "chat");
            AssertFocus(registry, "chat");
            Apply(registry, PanelCommandKind.SetPinned, "chat", true);
            Apply(registry, PanelCommandKind.Minimize, "chat");
            AssertFocus(registry, "document");
            Apply(registry, PanelCommandKind.Focus, "science-3d");
            AssertFocus(registry, "science-3d");
            Apply(registry, PanelCommandKind.Close, "science-3d");
            AssertFocus(registry, "document");
            Apply(registry, PanelCommandKind.Restore, "chat");
            AssertFocus(registry, "chat");
            Apply(registry, PanelCommandKind.Close, "document");
            AssertFocus(registry, "chat");
            Apply(registry, PanelCommandKind.Open, "notes");
            AssertFocus(registry, "notes");
            Apply(registry, PanelCommandKind.Minimize, "notes");
            AssertFocus(registry, "chat");
            Apply(registry, PanelCommandKind.Close, "chat");
            AssertFocus(registry, null);
            Apply(registry, PanelCommandKind.Restore, "notes");
            AssertFocus(registry, "notes");
            Apply(registry, PanelCommandKind.SetPinned, "notes", true);

            PanelSnapshot snapshot = registry.CreateSnapshot();
            Assert.That(registry.Version, Is.EqualTo(19));
            Assert.That(snapshot.Panels.Count, Is.EqualTo(4));
            Assert.That(snapshot.Panels[0].PanelId, Is.EqualTo("chat"));
            Assert.That(snapshot.Panels[1].PanelId, Is.EqualTo("document"));
            Assert.That(snapshot.Panels[2].PanelId, Is.EqualTo("science-3d"));
            Assert.That(snapshot.Panels[3].PanelId, Is.EqualTo("notes"));
            Assert.That(snapshot.Panels[3].IsFocused, Is.True);
            Assert.That(snapshot.Panels[3].IsPinned, Is.True);
        }

        [Test]
        public void DuplicateOpenAndInvalidLifecycleTransitionsFailClosed()
        {
            PanelRegistry registry = RegistryWithFourPanels();
            Apply(registry, PanelCommandKind.Open, "chat");
            long version = registry.Version;

            AssertRejected(
                registry,
                PanelCommand.For(PanelCommandKind.Open, version, "chat"),
                PanelApplyError.AlreadyInState,
                version
            );
            AssertRejected(
                registry,
                PanelCommand.For(PanelCommandKind.Focus, version, "notes"),
                PanelApplyError.PanelNotVisible,
                version
            );
            AssertRejected(
                registry,
                PanelCommand.For(PanelCommandKind.Restore, version, "notes"),
                PanelApplyError.PanelNotMinimized,
                version
            );
            AssertFocus(registry, "chat");
        }

        [Test]
        public void EventJournalIsBoundedAndSnapshotsVisibleIdsInSortedOrder()
        {
            PanelRegistry registry = RegistryWithFourPanels();
            Apply(registry, PanelCommandKind.Open, "notes");
            Apply(registry, PanelCommandKind.Open, "chat");
            IReadOnlyList<PanelLifecycleEvent> currentEvents = registry.Events();
            PanelLifecycleEvent last = currentEvents[currentEvents.Count - 1];
            Assert.That(last.VisiblePanelIds, Is.EqualTo(new[] { "chat", "notes" }));

            for (int index = 0; index < 70; index += 1)
            {
                Apply(
                    registry,
                    PanelCommandKind.SetPinned,
                    "chat",
                    index % 2 == 0
                );
            }

            IReadOnlyList<PanelLifecycleEvent> events = registry.Events();
            Assert.That(events.Count, Is.EqualTo(PanelRegistry.MaximumEvents));
            Assert.That(events[0].Sequence, Is.GreaterThan(1));
            Assert.That(events[^1].RegistryVersion, Is.EqualTo(registry.Version));
        }

        private static PanelRegistry RegistryWithFourPanels()
        {
            PanelRegistry registry = new PanelRegistry();
            AssertAccepted(
                registry,
                PanelCommand.Register(
                    registry.Version,
                    Definition("chat", PanelKind.Chat, -1.05f)
                )
            );
            AssertAccepted(
                registry,
                PanelCommand.Register(
                    registry.Version,
                    Definition("science-3d", PanelKind.Science3D, -0.35f)
                )
            );
            AssertAccepted(
                registry,
                PanelCommand.Register(
                    registry.Version,
                    Definition("document", PanelKind.Document, 0.35f)
                )
            );
            AssertAccepted(
                registry,
                PanelCommand.Register(
                    registry.Version,
                    Definition("notes", PanelKind.Notes, 1.05f)
                )
            );
            return registry;
        }

        private static PanelDefinition Definition(
            string panelId,
            PanelKind kind,
            float x
        )
        {
            Assert.That(
                PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    Bounds(x),
                    out PanelDefinition definition
                ),
                Is.True
            );
            return definition;
        }

        private static PanelBounds Bounds(float x)
        {
            return new PanelBounds(
                new Vector3(x, 0.0f, 2.5f),
                new Vector2(0.5f, 0.5f)
            );
        }

        private static void Apply(
            PanelRegistry registry,
            PanelCommandKind kind,
            string panelId,
            bool boolValue = false
        )
        {
            AssertAccepted(
                registry,
                PanelCommand.For(kind, registry.Version, panelId, boolValue)
            );
        }

        private static void AssertAccepted(
            PanelRegistry registry,
            PanelCommand command
        )
        {
            PanelApplyResult result = registry.Apply(command);
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
            Assert.That(result.Error, Is.EqualTo(PanelApplyError.None));
            Assert.That(result.Version, Is.EqualTo(registry.Version));
            Assert.That(result.LifecycleEvent, Is.Not.Null);
        }

        private static void AssertRejected(
            PanelRegistry registry,
            PanelCommand command,
            PanelApplyError expectedError,
            long expectedVersion
        )
        {
            PanelApplyResult result = registry.Apply(command);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error, Is.EqualTo(expectedError));
            Assert.That(registry.Version, Is.EqualTo(expectedVersion));
            Assert.That(result.LifecycleEvent, Is.Null);
        }

        private static void AssertFocus(PanelRegistry registry, string expectedPanelId)
        {
            int focused = 0;
            foreach (PanelState state in registry.CreateSnapshot().Panels)
            {
                if (state.IsFocused)
                {
                    focused += 1;
                    Assert.That(state.PanelId, Is.EqualTo(expectedPanelId));
                    Assert.That(state.Visibility, Is.EqualTo(PanelVisibility.Visible));
                }
            }

            Assert.That(focused, Is.EqualTo(expectedPanelId == null ? 0 : 1));
        }
    }
}
