using System;
using System.Collections.Generic;
using NUnit.Framework;
using OmniLensXR.S05;
using OmniLensXR.S06;
using UnityEngine;

namespace OmniLensXR.Tests.Editor
{
    public sealed class S06WorkspaceTests
    {
        [Test]
        public void TwentyPlacementFixturesKeepPrimaryFullyInsideAndNonOverlapping()
        {
            IReadOnlyList<S06PlacementFixture> fixtures =
                S06PlacementFixtureFactory.CreateTwenty();
            Assert.That(fixtures.Count, Is.EqualTo(20));
            for (int index = 0; index < fixtures.Count; index += 1)
            {
                S06PlacementFixture fixture = fixtures[index];
                Assert.That(fixture.Index, Is.EqualTo(index));
                Assert.That(
                    PanelPlacementService.IsFullyInsideSafeZone(fixture.Primary),
                    Is.True
                );
                Assert.That(
                    PanelPlacementService.IsSnapped(fixture.Primary),
                    Is.True
                );
                foreach (PanelBounds occupied in fixture.Occupied)
                {
                    Assert.That(
                        PanelPlacementService.Overlaps(fixture.Primary, occupied),
                        Is.False
                    );
                }
            }
        }

        [Test]
        public void PlacementClampsSnapsAndDeterministicallyAvoidsCollision()
        {
            PanelBounds obstacle = new PanelBounds(
                new Vector3(0.0f, 0.0f, 2.5f),
                new Vector2(0.65f, 0.65f)
            );
            IReadOnlyList<PanelBounds> occupied = new[] { obstacle };
            PanelPlacementResult first = PanelPlacementService.Resolve(
                occupied,
                new Vector3(0.01f, 0.01f, 2.49f),
                new Vector2(0.66f, 0.64f)
            );
            PanelPlacementResult second = PanelPlacementService.Resolve(
                occupied,
                new Vector3(0.01f, 0.01f, 2.49f),
                new Vector2(0.66f, 0.64f)
            );

            Assert.That(first.Accepted, Is.True, first.Error.ToString());
            Assert.That(second.Accepted, Is.True, second.Error.ToString());
            Assert.That(
                first.Bounds.PositionMeters,
                Is.EqualTo(second.Bounds.PositionMeters)
            );
            Assert.That(PanelPlacementService.IsSnapped(first.Bounds), Is.True);
            Assert.That(PanelPlacementService.Overlaps(first.Bounds, obstacle), Is.False);

            PanelPlacementResult edge = PanelPlacementService.Resolve(
                Array.Empty<PanelBounds>(),
                new Vector3(100.0f, 100.0f, 100.0f),
                new Vector2(100.0f, 100.0f)
            );
            Assert.That(edge.Accepted, Is.True, edge.Error.ToString());
            Assert.That(
                edge.Bounds.SizeMeters.x,
                Is.EqualTo(S04.SpatialUiTokens.MaximumPanelWidthMeters).Within(0.0001f)
            );
            Assert.That(
                edge.Bounds.SizeMeters.y,
                Is.EqualTo(S04.SpatialUiTokens.MaximumPanelHeightMeters).Within(0.0001f)
            );
            Assert.That(PanelPlacementService.IsFullyInsideSafeZone(edge.Bounds), Is.True);
        }

        [Test]
        public void PlacementRejectsNonFiniteAndImpossiblePlane()
        {
            PanelPlacementResult invalid = PanelPlacementService.Resolve(
                Array.Empty<PanelBounds>(),
                new Vector3(float.NaN, 0.0f, 2.5f),
                new Vector2(0.5f, 0.5f)
            );
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(PanelPlacementError.InvalidInput));

            PanelPlacementResult impossible = PanelPlacementService.Resolve(
                Array.Empty<PanelBounds>(),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector2(2.4f, 1.6f)
            );
            Assert.That(impossible.Accepted, Is.False);
            Assert.That(
                impossible.Error,
                Is.EqualTo(PanelPlacementError.OutsideSupportedPlane)
            );
        }

        [Test]
        public void RegistryMoveResizePreserveFocusPinAndRejectOverlap()
        {
            PanelRegistry registry = new PanelRegistry();
            Register(registry, "chat", PanelKind.Chat, -0.7f);
            Register(registry, "document", PanelKind.Document, 0.7f);
            Apply(registry, PanelCommandKind.Open, "chat");
            Apply(registry, PanelCommandKind.Open, "document");
            Apply(registry, PanelCommandKind.SetPinned, "document", true);
            AssertAccepted(
                registry,
                PanelCommand.Move(
                    registry.Version,
                    "document",
                    new Vector3(0.8f, 0.0f, 2.5f)
                )
            );
            AssertAccepted(
                registry,
                PanelCommand.Resize(
                    registry.Version,
                    "document",
                    new Vector2(0.6f, 0.5f)
                )
            );
            Assert.That(registry.TryGet("document", out PanelState document), Is.True);
            Assert.That(document.IsFocused, Is.True);
            Assert.That(document.IsPinned, Is.True);
            Assert.That(document.Bounds.PositionMeters.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(document.Bounds.SizeMeters.x, Is.EqualTo(0.6f).Within(0.001f));

            long stableVersion = registry.Version;
            PanelApplyResult overlap = registry.Apply(
                PanelCommand.Move(
                    stableVersion,
                    "document",
                    new Vector3(-0.2f, 0.0f, 2.5f)
                )
            );
            Assert.That(overlap.Accepted, Is.False);
            Assert.That(overlap.Error, Is.EqualTo(PanelApplyError.PanelOverlap));
            Assert.That(registry.Version, Is.EqualTo(stableVersion));
            PanelApplyResult invalid = registry.Apply(
                PanelCommand.Move(
                    stableVersion,
                    "document",
                    new Vector3(float.PositiveInfinity, 0.0f, 2.5f)
                )
            );
            Assert.That(invalid.Accepted, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(PanelApplyError.InvalidBounds));
            Assert.That(registry.Version, Is.EqualTo(stableVersion));
        }

        [Test]
        public void LocalizationResolvesAllKeysAndSupportsEnglishFallback()
        {
            SpatialStringCatalog catalog = new SpatialStringCatalog();
            foreach (SpatialStringKey key in Enum.GetValues(typeof(SpatialStringKey)))
            {
                Assert.That(catalog.TryGet(key, out LocalizedText english), Is.True);
                Assert.That(english.Value, Is.Not.Empty);
                Assert.That(english.UsedEnglishFallback, Is.False);
            }

            Assert.That(catalog.TrySetLanguage(SpatialLanguage.Vietnamese), Is.True);
            foreach (SpatialStringKey key in Enum.GetValues(typeof(SpatialStringKey)))
            {
                Assert.That(catalog.TryGet(key, out LocalizedText vietnamese), Is.True);
                Assert.That(vietnamese.Value, Is.Not.Empty);
                Assert.That(vietnamese.UsedEnglishFallback, Is.False);
            }

            Dictionary<SpatialStringKey, string> englishValues =
                new Dictionary<SpatialStringKey, string>();
            Assert.That(catalog.TrySetLanguage(SpatialLanguage.English), Is.True);
            foreach (SpatialStringKey key in Enum.GetValues(typeof(SpatialStringKey)))
            {
                Assert.That(catalog.TryGet(key, out LocalizedText value), Is.True);
                englishValues[key] = value.Value;
            }

            SpatialStringCatalog fallback = new SpatialStringCatalog(
                englishValues,
                new Dictionary<SpatialStringKey, string>()
            );
            Assert.That(fallback.TrySetLanguage(SpatialLanguage.Vietnamese), Is.True);
            Assert.That(
                fallback.TryGet(SpatialStringKey.Document, out LocalizedText result),
                Is.True
            );
            Assert.That(result.Value, Is.EqualTo("Document"));
            Assert.That(result.UsedEnglishFallback, Is.True);
        }

        [Test]
        public void LocalizationFormatsDatesAndNumbersByRuntimeLanguage()
        {
            SpatialStringCatalog catalog = new SpatialStringCatalog();
            DateTimeOffset date = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
            string englishNumber = catalog.FormatNumber(1234.5);
            string englishDate = catalog.FormatDate(date);
            Assert.That(catalog.TrySetLanguage(SpatialLanguage.Vietnamese), Is.True);
            string vietnameseNumber = catalog.FormatNumber(1234.5);
            string vietnameseDate = catalog.FormatDate(date);
            Assert.That(vietnameseNumber, Is.Not.EqualTo(englishNumber));
            Assert.That(vietnameseDate, Is.Not.EqualTo(englishDate));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.FormatNumber(double.NaN));
        }

        [Test]
        public void SyntheticFatigueProtocolIsBoundedMonotonicAndNeverPhysicalPass()
        {
            S06FatigueProtocol protocol = new S06FatigueProtocol();
            Assert.That(protocol.Start(0.0), Is.True);
            Assert.That(
                protocol.Record(FatigueInteractionKind.LongTextEntryAttempt, 0.1),
                Is.EqualTo(FatigueRecordError.LongTextEntryProhibited)
            );
            Assert.That(
                protocol.Record(FatigueInteractionKind.Selection, 0.05),
                Is.EqualTo(FatigueRecordError.NonMonotonicTimestamp)
            );
            for (int index = 0; index < 599; index += 1)
            {
                Assert.That(
                    protocol.Record(
                        FatigueInteractionKind.Selection,
                        0.2 + index * 0.5
                    ),
                    Is.EqualTo(FatigueRecordError.None)
                );
            }

            Assert.That(
                protocol.Record(FatigueInteractionKind.HeadTurn, 300.0),
                Is.EqualTo(FatigueRecordError.SampleLimitReached)
            );
            Assert.That(protocol.Advance(600.0), Is.EqualTo(FatigueRecordError.None));
            FatigueProtocolSummary summary = protocol.Summary();
            Assert.That(summary.State, Is.EqualTo(FatigueProtocolState.CompletedSynthetic));
            Assert.That(summary.ElapsedSeconds, Is.EqualTo(600.0));
            Assert.That(summary.Selections, Is.EqualTo(599));
            Assert.That(summary.ProhibitedLongTextAttempts, Is.EqualTo(1));
            Assert.That(summary.PhysicalViewerResult, Is.EqualTo("NOT_RUN"));
            Assert.That(
                S06FatigueMetricLog.Format(
                    new FatigueProtocolSummary(
                        FatigueProtocolState.Running,
                        5.0,
                        1,
                        0,
                        1,
                        0,
                        0
                    )
                ),
                Is.EqualTo(
                    "[S06_METRICS] state=Running elapsed=5.0 physical=NOT_RUN"
                )
            );
            Assert.That(
                PanelManipulationTokens.MinimumHandleHitSizeMeters,
                Is.GreaterThanOrEqualTo(0.12f)
            );
        }

        private static void Register(
            PanelRegistry registry,
            string panelId,
            PanelKind kind,
            float x
        )
        {
            Assert.That(
                PanelDefinition.TryCreate(
                    panelId,
                    kind,
                    new PanelBounds(
                        new Vector3(x, 0.0f, 2.5f),
                        new Vector2(0.5f, 0.5f)
                    ),
                    out PanelDefinition definition
                ),
                Is.True
            );
            AssertAccepted(
                registry,
                PanelCommand.Register(registry.Version, definition)
            );
        }

        private static void Apply(
            PanelRegistry registry,
            PanelCommandKind kind,
            string panelId,
            bool value = false
        )
        {
            AssertAccepted(
                registry,
                PanelCommand.For(kind, registry.Version, panelId, value)
            );
        }

        private static void AssertAccepted(
            PanelRegistry registry,
            PanelCommand command
        )
        {
            PanelApplyResult result = registry.Apply(command);
            Assert.That(result.Accepted, Is.True, result.Error.ToString());
        }
    }
}
