using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using OmniLensXR.S05;
using OmniLensXR.S07;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S07WorkspaceTests
    {
        [Test]
        public void CurrentSchemaRoundTripsAndRestoresThreePanels()
        {
            WorkspaceLayoutData source = S07WorkspaceDefaults.Create();
            Assert.That(
                WorkspaceLayoutCodec.TrySerialize(
                    source,
                    out string json,
                    out WorkspaceLayoutError serializeError
                ),
                Is.True,
                serializeError.ToString()
            );
            Assert.That(
                WorkspaceLayoutCodec.TryDeserialize(
                    json,
                    out WorkspaceLayoutData loaded,
                    out WorkspaceLayoutError loadError
                ),
                Is.True,
                loadError.ToString()
            );
            Assert.That(loaded.schemaVersion, Is.EqualTo(2));
            Assert.That(loaded.panels.Length, Is.EqualTo(3));
            Assert.That(loaded.selectedApp, Is.EqualTo("Home"));
            Assert.That(
                WorkspaceLayoutCodec.TryRestoreRegistry(
                    loaded,
                    out PanelRegistry registry,
                    out WorkspaceLayoutError restoreError
                ),
                Is.True,
                restoreError.ToString()
            );
            Assert.That(registry.Count, Is.EqualTo(3));
            Assert.That(registry.TryGet("document", out PanelState document), Is.True);
            Assert.That(document.IsFocused, Is.True);
            Assert.That(document.IsPinned, Is.True);
            Assert.That(document.Bounds.PositionMeters.x, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void LegacySchemaMigratesToV2WithSafeDefaults()
        {
            const string legacy =
                "{\"schemaVersion\":1,\"selectedApp\":\"Science3D\","
                + "\"savedUnixMilliseconds\":42,\"panels\":[{"
                + "\"panelId\":\"science-3d\",\"kind\":2,\"visibility\":2,"
                + "\"positionX\":0,\"positionY\":0,\"positionZ\":2.5,"
                + "\"width\":0.5,\"height\":0.5,\"zOrder\":1,"
                + "\"isFocused\":true}]}";
            Assert.That(
                WorkspaceLayoutCodec.TryDeserialize(
                    legacy,
                    out WorkspaceLayoutData migrated,
                    out WorkspaceLayoutError error
                ),
                Is.True,
                error.ToString()
            );
            Assert.That(migrated.schemaVersion, Is.EqualTo(2));
            Assert.That(migrated.selectedApp, Is.EqualTo("Science3D"));
            Assert.That(migrated.panels[0].isPinned, Is.False);
            Assert.That(migrated.savedUnixMilliseconds, Is.EqualTo(42));
        }

        [Test]
        public void UnknownVersionInvalidFocusAndDuplicatesFailClosed()
        {
            Assert.That(
                WorkspaceLayoutCodec.TryDeserialize(
                    "{\"schemaVersion\":999}",
                    out _,
                    out WorkspaceLayoutError versionError
                ),
                Is.False
            );
            Assert.That(versionError, Is.EqualTo(WorkspaceLayoutError.UnsupportedVersion));

            WorkspaceLayoutData invalidFocus = S07WorkspaceDefaults.Create();
            invalidFocus.panels[0].isFocused = true;
            invalidFocus.panels[2].isFocused = true;
            Assert.That(
                WorkspaceLayoutCodec.TryValidate(invalidFocus, out WorkspaceLayoutError focusError),
                Is.False
            );
            Assert.That(focusError, Is.EqualTo(WorkspaceLayoutError.InvalidFocus));

            WorkspaceLayoutData duplicate = S07WorkspaceDefaults.Create();
            duplicate.panels[1].panelId = duplicate.panels[0].panelId;
            Assert.That(
                WorkspaceLayoutCodec.TryValidate(duplicate, out WorkspaceLayoutError duplicateError),
                Is.False
            );
            Assert.That(duplicateError, Is.EqualTo(WorkspaceLayoutError.DuplicatePanelId));
        }

        [Test]
        public void AuthenticatedEnvelopeRejectsTamperWrongKeyAndTruncation()
        {
            byte[] key = CreateKey(1);
            byte[] envelope = WorkspaceEnvelope.Encrypt(
                Encoding.UTF8.GetBytes("validated workspace"),
                key
            );
            Assert.That(
                WorkspaceEnvelope.TryDecrypt(
                    envelope,
                    key,
                    out byte[] plaintext,
                    out WorkspaceRepositoryError error
                ),
                Is.True,
                error.ToString()
            );
            Assert.That(Encoding.UTF8.GetString(plaintext), Is.EqualTo("validated workspace"));

            byte[] tampered = (byte[])envelope.Clone();
            tampered[tampered.Length - 33] ^= 0x01;
            Assert.That(
                WorkspaceEnvelope.TryDecrypt(tampered, key, out _, out error),
                Is.False
            );
            Assert.That(error, Is.EqualTo(WorkspaceRepositoryError.AuthenticationFailed));
            Assert.That(
                WorkspaceEnvelope.TryDecrypt(
                    envelope,
                    CreateKey(2),
                    out _,
                    out error
                ),
                Is.False
            );
            Assert.That(error, Is.EqualTo(WorkspaceRepositoryError.AuthenticationFailed));
            Assert.That(
                WorkspaceEnvelope.TryDecrypt(new byte[8], key, out _, out error),
                Is.False
            );
            Assert.That(error, Is.EqualTo(WorkspaceRepositoryError.InvalidEnvelope));
        }

        [Test]
        public void SandboxRepositoryRoundTripsRejectsTamperAndResets()
        {
            string root = NewTempRoot();
            try
            {
                SandboxWorkspaceRepository repository =
                    new SandboxWorkspaceRepository(root);
                Assert.That(
                    repository.TryLoad(out _, out WorkspaceRepositoryError missingError),
                    Is.False
                );
                Assert.That(missingError, Is.EqualTo(WorkspaceRepositoryError.Missing));
                Assert.That(
                    repository.TrySave(
                        S07WorkspaceDefaults.Create(),
                        out WorkspaceRepositoryError saveError
                    ),
                    Is.True,
                    saveError.ToString()
                );
                Assert.That(repository.TryLoad(out WorkspaceLayoutData loaded, out _), Is.True);
                Assert.That(loaded.panels.Length, Is.EqualTo(3));

                string layoutPath = Path.Combine(
                    root,
                    SandboxWorkspaceRepository.LayoutFileName
                );
                byte[] bytes = File.ReadAllBytes(layoutPath);
                bytes[bytes.Length - 1] ^= 0x01;
                File.WriteAllBytes(layoutPath, bytes);
                Assert.That(
                    repository.TryLoad(out _, out WorkspaceRepositoryError tamperError),
                    Is.False
                );
                Assert.That(
                    tamperError,
                    Is.EqualTo(WorkspaceRepositoryError.AuthenticationFailed)
                );
                Assert.That(repository.TryReset(out _), Is.True);
                Assert.That(repository.TryLoad(out _, out missingError), Is.False);
                Assert.That(missingError, Is.EqualTo(WorkspaceRepositoryError.Missing));
            }
            finally
            {
                DeleteTempRoot(root);
            }
        }

        [Test]
        public void SandboxRepositoryRejectsRelativeAndFilesystemRootPaths()
        {
            Assert.Throws<ArgumentException>(
                () => new SandboxWorkspaceRepository("relative/sandbox")
            );
            Assert.Throws<ArgumentException>(
                () =>
                    new SandboxWorkspaceRepository(
                        Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()))
                    )
            );
        }

        [Test]
        public void OfflineShellKeepsLocalAppsAvailableAndRejectsAgent()
        {
            string root = NewTempRoot();
            try
            {
                SandboxWorkspaceRepository repository =
                    new SandboxWorkspaceRepository(root);
                OfflineWorkspaceShell shell = new OfflineWorkspaceShell(
                    repository,
                    S07WorkspaceDefaults.Create
                );
                shell.SetNetworkState(WorkspaceNetworkState.Offline);
                AssertAccepted(shell.Apply(OfflineShellAction.OpenScience3D));
                Assert.That(shell.SelectedApp, Is.EqualTo(WorkspaceApp.Science3D));
                AssertAccepted(shell.Apply(OfflineShellAction.OpenSettings));
                OfflineShellResult denied = shell.Apply(OfflineShellAction.OpenAgent);
                Assert.That(denied.Accepted, Is.False);
                Assert.That(denied.Error, Is.EqualTo(OfflineShellError.BackendUnavailable));
                Assert.That(shell.SelectedApp, Is.EqualTo(WorkspaceApp.Settings));
                AssertAccepted(shell.Apply(OfflineShellAction.CompleteOnboarding));
                AssertAccepted(shell.Apply(OfflineShellAction.ResetWorkspace));
                Assert.That(shell.SelectedApp, Is.EqualTo(WorkspaceApp.Home));
                Assert.That(shell.OnboardingComplete, Is.False);
                Assert.That(repository.TryLoad(out WorkspaceLayoutData reset, out _), Is.True);
                Assert.That(reset.panels.Length, Is.EqualTo(3));
                Assert.That(S07InputFallback.Resolve(false), Is.EqualTo(SpatialInputFallback.GazeDwell));
            }
            finally
            {
                DeleteTempRoot(root);
            }
        }

        [Test]
        public void OfflineShellInvalidAndFailedResetPreserveCurrentState()
        {
            OfflineWorkspaceShell shell = new OfflineWorkspaceShell(
                new FailingRepository(),
                S07WorkspaceDefaults.Create
            );
            shell.SetNetworkState(WorkspaceNetworkState.Offline);
            AssertAccepted(shell.Apply(OfflineShellAction.OpenSettings));
            AssertAccepted(shell.Apply(OfflineShellAction.CompleteOnboarding));

            OfflineShellResult invalid = shell.Apply((OfflineShellAction)999);
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(OfflineShellError.InvalidAction));
            Assert.That(invalid.SelectedApp, Is.EqualTo(WorkspaceApp.Settings));
            Assert.That(invalid.OnboardingComplete, Is.True);

            OfflineShellResult reset = shell.Apply(OfflineShellAction.ResetWorkspace);
            Assert.That(reset.Accepted, Is.False);
            Assert.That(reset.Error, Is.EqualTo(OfflineShellError.PersistenceFailure));
            Assert.That(reset.SelectedApp, Is.EqualTo(WorkspaceApp.Settings));
            Assert.That(reset.OnboardingComplete, Is.True);
        }

        [Test]
        public void ThirtyActivityFixturesRenderEmptyErrorAndAllStatuses()
        {
            IReadOnlyList<AgentActivityFixture> fixtures =
                AgentActivityFixtureFactory.CreateThirty();
            Assert.That(fixtures.Count, Is.EqualTo(30));
            HashSet<AgentActivityStatus> statuses = new HashSet<AgentActivityStatus>();
            for (int index = 0; index < fixtures.Count; index += 1)
            {
                Assert.That(fixtures[index].Index, Is.EqualTo(index));
                Assert.That(fixtures[index].Timeline.RenderSummary(), Is.Not.Empty);
                foreach (AgentActivityEvent item in fixtures[index].Timeline.Events)
                {
                    statuses.Add(item.Status);
                }
            }

            Assert.That(fixtures[0].ExpectedKind, Is.EqualTo("empty"));
            Assert.That(fixtures[0].Timeline.RenderSummary(), Is.EqualTo("No agent activity"));
            Assert.That(fixtures[1].ExpectedKind, Is.EqualTo("error"));
            Assert.That(fixtures[1].Timeline.LastError, Is.EqualTo(AgentTimelineError.StreamError));
            Assert.That(statuses, Does.Contain(AgentActivityStatus.Pending));
            Assert.That(statuses, Does.Contain(AgentActivityStatus.Accepted));
            Assert.That(statuses, Does.Contain(AgentActivityStatus.Failed));
            Assert.That(statuses, Does.Contain(AgentActivityStatus.Cancelled));
        }

        [Test]
        public void ActivityTimelineRejectsInvalidDuplicateAndOverflowEvents()
        {
            AgentActivityTimeline timeline = new AgentActivityTimeline();
            Assert.That(
                timeline.Append(
                    new AgentActivityEvent("bad id", AgentActivityStatus.Pending, "Bad")
                ),
                Is.EqualTo(AgentTimelineError.InvalidEvent)
            );
            for (int index = 0; index < AgentActivityTimeline.MaximumEvents; index += 1)
            {
                Assert.That(
                    timeline.Append(
                        new AgentActivityEvent(
                            $"command-{index:D2}",
                            AgentActivityStatus.Accepted,
                            "Fixture"
                        )
                    ),
                    Is.EqualTo(AgentTimelineError.None)
                );
            }

            Assert.That(
                timeline.Append(
                    new AgentActivityEvent(
                        "overflow",
                        AgentActivityStatus.Accepted,
                        "Overflow"
                    )
                ),
                Is.EqualTo(AgentTimelineError.CapacityReached)
            );
            AgentActivityTimeline duplicate = new AgentActivityTimeline();
            AgentActivityEvent item = new AgentActivityEvent(
                "same",
                AgentActivityStatus.Pending,
                "Fixture"
            );
            Assert.That(duplicate.Append(item), Is.EqualTo(AgentTimelineError.None));
            Assert.That(duplicate.Append(item), Is.EqualTo(AgentTimelineError.DuplicateId));
        }

        private static byte[] CreateKey(byte seed)
        {
            byte[] key = new byte[64];
            for (int index = 0; index < key.Length; index += 1)
            {
                key[index] = (byte)(seed + index);
            }

            return key;
        }

        private static string NewTempRoot()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "omnilens-s07-tests-" + Guid.NewGuid().ToString("N")
            );
        }

        private static void DeleteTempRoot(string root)
        {
            string full = Path.GetFullPath(root);
            string temp = Path.GetFullPath(Path.GetTempPath());
            if (full.StartsWith(temp, StringComparison.Ordinal) && Directory.Exists(full))
            {
                Directory.Delete(full, true);
            }
        }

        private static void AssertAccepted(OfflineShellResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }

        private sealed class FailingRepository : IWorkspaceRepository
        {
            public bool TrySave(
                WorkspaceLayoutData data,
                out WorkspaceRepositoryError error
            )
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }

            public bool TryLoad(
                out WorkspaceLayoutData data,
                out WorkspaceRepositoryError error
            )
            {
                data = null;
                error = WorkspaceRepositoryError.Missing;
                return false;
            }

            public bool TryReset(out WorkspaceRepositoryError error)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
        }
    }
}
