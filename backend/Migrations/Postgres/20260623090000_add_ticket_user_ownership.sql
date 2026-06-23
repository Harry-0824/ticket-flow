ALTER TABLE "Tickets"
    ADD COLUMN IF NOT EXISTS "UserId" integer;

CREATE INDEX IF NOT EXISTS "IX_Tickets_UserId"
    ON "Tickets" ("UserId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_Tickets_ApplicationUsers_UserId'
    ) THEN
        ALTER TABLE "Tickets"
            ADD CONSTRAINT "FK_Tickets_ApplicationUsers_UserId"
            FOREIGN KEY ("UserId")
            REFERENCES "ApplicationUsers" ("Id")
            ON DELETE CASCADE;
    END IF;
END $$;
