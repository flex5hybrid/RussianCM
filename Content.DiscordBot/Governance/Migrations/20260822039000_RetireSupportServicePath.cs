using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.DiscordBot.Governance.Migrations;

[DbContext(typeof(GovernanceDbContext))]
[Migration("20260822039000_RetireSupportServicePath")]
public sealed class RetireSupportServicePath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Player support is no longer a standalone Governance service path. AHelp work belongs
            -- to moderation. Immutable reputation observations are intentionally left untouched;
            -- application-side Reputation v2 folds historical support observations into moderation.

            DELETE FROM governance.service_paths
            WHERE track = 'support';

            -- If support occupied the primary slot, preserve the user's remaining selected path by
            -- promoting the old secondary path without treating this migration as a user path change.
            UPDATE governance.service_paths AS path
            SET slot = 1
            WHERE path.slot = 2
              AND NOT EXISTS (
                  SELECT 1
                  FROM governance.service_paths AS primary_path
                  WHERE primary_path.user_id = path.user_id
                    AND primary_path.slot = 1);

            DELETE FROM governance.qualifications
            WHERE track = 'support';

            DELETE FROM governance.reputation_snapshots
            WHERE track = 'support';

            -- service_paths has deferred constraint triggers. The DELETE/UPDATE operations above
            -- queue trigger events, and PostgreSQL refuses to ALTER the table while those events
            -- are still pending (SQLSTATE 55006). Flush them while keeping the migration atomic.
            SET CONSTRAINTS ALL IMMEDIATE;

            ALTER TABLE governance.service_paths
                DROP CONSTRAINT IF EXISTS service_paths_active_track_check;
            ALTER TABLE governance.service_paths
                ADD CONSTRAINT service_paths_active_track_check
                CHECK (track IN ('moderation', 'jury', 'event', 'contributor'));

            -- Keep the generic qualification/path invariant aligned with the active path set.
            CREATE OR REPLACE FUNCTION governance.require_service_path_for_qualification()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $governance$
            BEGIN
                IF NEW.track IN ('moderation', 'jury', 'event', 'contributor')
                   AND NEW.level > 0
                   AND NOT EXISTS (
                       SELECT 1
                       FROM governance.service_paths AS path
                       WHERE path.user_id = NEW.user_id
                         AND path.track = NEW.track) THEN
                    RAISE EXCEPTION 'qualification % requires selected service path for user %', NEW.track, NEW.user_id
                        USING ERRCODE = '23514';
                END IF;
                RETURN NULL;
            END;
            $governance$;

            CREATE OR REPLACE FUNCTION governance.demote_qualification_after_service_path_change()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $governance$
            DECLARE
                affected_user uuid;
                affected_track text;
            BEGIN
                affected_user := OLD.user_id;
                affected_track := OLD.track;

                IF affected_track NOT IN ('moderation', 'jury', 'event', 'contributor') THEN
                    RETURN NULL;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM governance.service_paths AS path
                    WHERE path.user_id = affected_user
                      AND path.track = affected_track) THEN
                    RETURN NULL;
                END IF;

                UPDATE governance.qualifications
                SET level = 0,
                    updated_at = now()
                WHERE user_id = affected_user
                  AND track = affected_track
                  AND level > 0;

                RETURN NULL;
            END;
            $governance$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE governance.service_paths
                DROP CONSTRAINT IF EXISTS service_paths_active_track_check;

            -- Down only restores schema compatibility. Removed user path selections cannot be
            -- reconstructed without inventing user intent, so they deliberately remain removed.
            CREATE OR REPLACE FUNCTION governance.require_service_path_for_qualification()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $governance$
            BEGIN
                IF NEW.track IN ('support', 'moderation', 'jury', 'event', 'contributor')
                   AND NEW.level > 0
                   AND NOT EXISTS (
                       SELECT 1
                       FROM governance.service_paths AS path
                       WHERE path.user_id = NEW.user_id
                         AND path.track = NEW.track) THEN
                    RAISE EXCEPTION 'qualification % requires selected service path for user %', NEW.track, NEW.user_id
                        USING ERRCODE = '23514';
                END IF;
                RETURN NULL;
            END;
            $governance$;

            CREATE OR REPLACE FUNCTION governance.demote_qualification_after_service_path_change()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $governance$
            DECLARE
                affected_user uuid;
                affected_track text;
            BEGIN
                affected_user := OLD.user_id;
                affected_track := OLD.track;

                IF affected_track NOT IN ('support', 'moderation', 'jury', 'event', 'contributor') THEN
                    RETURN NULL;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM governance.service_paths AS path
                    WHERE path.user_id = affected_user
                      AND path.track = affected_track) THEN
                    RETURN NULL;
                END IF;

                UPDATE governance.qualifications
                SET level = 0,
                    updated_at = now()
                WHERE user_id = affected_user
                  AND track = affected_track
                  AND level > 0;

                RETURN NULL;
            END;
            $governance$;
            """);
    }
}
