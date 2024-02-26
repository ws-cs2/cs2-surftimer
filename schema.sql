
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

CREATE SCHEMA IF NOT EXISTS "public";

ALTER SCHEMA "public" OWNER TO "pg_database_owner";

CREATE OR REPLACE FUNCTION "public"."insert_into_recent_records_before"() RETURNS "trigger"
    LANGUAGE "plpgsql"
AS $$
DECLARE
    best_record public.records%ROWTYPE;
BEGIN
    -- Find the current best record (either first time or lowest ticks) for the same mapname and route
    SELECT INTO best_record *
    FROM public.records
    WHERE mapname = NEW.mapname
      AND route = NEW.route
    ORDER BY ticks ASC
    LIMIT 1;

    -- Check if the current record is the best time and best_record is not NULL
    IF NEW.ticks < COALESCE(best_record.ticks, NEW.ticks + 1) THEN

        -- Store the previous best time's name and ticks in recent_records
        INSERT INTO public.recent_records (
            steam_id,
            mapname,
            route,
            ticks,
            name,
            checkpoints,
            new_name,
            new_ticks,
            old_name,
            old_ticks,
            old_steam_id
        ) VALUES (
                     NEW.steam_id,
                     NEW.mapname,
                     NEW.route,
                     NEW.ticks,
                     NEW.name,
                     NEW.checkpoints,
                     NEW.name,
                     NEW.ticks,
                     best_record.name,
                     best_record.ticks,
                     best_record.steam_id
                 );
    END IF;

    RETURN NEW;
END;
$$;

ALTER FUNCTION "public"."insert_into_recent_records_before"() OWNER TO "postgres";

SET default_tablespace = '';

SET default_table_access_method = "heap";

CREATE TABLE IF NOT EXISTS "public"."global_cfg" (
                                                     "id" bigint NOT NULL,
                                                     "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
                                                     "command" "text" NOT NULL
);

ALTER TABLE "public"."global_cfg" OWNER TO "postgres";

ALTER TABLE "public"."global_cfg" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."global_cfg_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
    );

CREATE TABLE IF NOT EXISTS "public"."map_cfg" (
                                                  "id" integer NOT NULL,
                                                  "mapname" character varying(255) NOT NULL,
                                                  "command" character varying(255) NOT NULL
);

ALTER TABLE "public"."map_cfg" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."map_cfg_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."map_cfg_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."map_cfg_id_seq" OWNED BY "public"."map_cfg"."id";

CREATE TABLE IF NOT EXISTS "public"."maps" (
                                               "id" integer NOT NULL,
                                               "name" character varying(255) NOT NULL,
                                               "routes" "jsonb" NOT NULL
);

ALTER TABLE "public"."maps" OWNER TO "postgres";

CREATE TABLE IF NOT EXISTS "public"."maps_2" (
                                                 "id" integer NOT NULL,
                                                 "name" character varying(255) NOT NULL,
                                                 "route" character varying(255) NOT NULL,
                                                 "route_data" "jsonb" NOT NULL
);

ALTER TABLE "public"."maps_2" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."maps_2_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."maps_2_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."maps_2_id_seq" OWNED BY "public"."maps_2"."id";

CREATE SEQUENCE IF NOT EXISTS "public"."maps_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."maps_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."maps_id_seq" OWNED BY "public"."maps"."id";

CREATE TABLE IF NOT EXISTS "public"."players" (
                                                  "id" integer NOT NULL,
                                                  "steam_id" character varying(255) NOT NULL,
                                                  "points" integer NOT NULL,
                                                  "is_admin" boolean DEFAULT false NOT NULL,
                                                  "name" "text" NOT NULL,
                                                  "custom_hud_url" "text",
                                                  "is_vip" boolean DEFAULT false NOT NULL,
                                                  "custom_tag" "text",
                                                  "chat_color" "text",
                                                  "name_color" "text"
);

ALTER TABLE "public"."players" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."players_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."players_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."players_id_seq" OWNED BY "public"."players"."id";

CREATE TABLE IF NOT EXISTS "public"."recent_records" (
                                                         "id" integer NOT NULL,
                                                         "steam_id" character varying(255) NOT NULL,
                                                         "mapname" character varying(255) NOT NULL,
                                                         "route" character varying(255) NOT NULL,
                                                         "ticks" integer NOT NULL,
                                                         "name" "text" NOT NULL,
                                                         "checkpoints" "jsonb",
                                                         "new_name" "text" NOT NULL,
                                                         "new_ticks" integer NOT NULL,
                                                         "old_name" "text",
                                                         "old_ticks" integer,
                                                         "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
                                                         "old_steam_id" "text"
);

ALTER TABLE "public"."recent_records" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."recent_records_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."recent_records_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."recent_records_id_seq" OWNED BY "public"."recent_records"."id";

CREATE TABLE IF NOT EXISTS "public"."records" (
                                                  "id" integer NOT NULL,
                                                  "steam_id" character varying(255) NOT NULL,
                                                  "mapname" character varying(255) NOT NULL,
                                                  "route" character varying(255) NOT NULL,
                                                  "ticks" integer NOT NULL,
                                                  "name" "text" NOT NULL,
                                                  "velocity_start_xy" numeric NOT NULL,
                                                  "velocity_start_z" numeric NOT NULL,
                                                  "velocity_start_xyz" numeric NOT NULL,
                                                  "velocity_end_xy" numeric NOT NULL,
                                                  "velocity_end_z" numeric NOT NULL,
                                                  "velocity_end_xyz" numeric NOT NULL,
                                                  "checkpoints" "jsonb",
                                                  "style" "text" DEFAULT 'normal'::"text" NOT NULL
);

ALTER TABLE "public"."records" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."records_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."records_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."records_id_seq" OWNED BY "public"."records"."id";

CREATE TABLE IF NOT EXISTS "public"."replays" (
                                                  "id" integer NOT NULL,
                                                  "steam_id" character varying(255) NOT NULL,
                                                  "mapname" character varying(255) NOT NULL,
                                                  "route" character varying(255) NOT NULL,
                                                  "ticks" integer NOT NULL,
                                                  "replay_url" "text",
                                                  "style" "text" DEFAULT 'normal'::"text" NOT NULL
);

ALTER TABLE "public"."replays" OWNER TO "postgres";

CREATE SEQUENCE IF NOT EXISTS "public"."replays_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER TABLE "public"."replays_id_seq" OWNER TO "postgres";

ALTER SEQUENCE "public"."replays_id_seq" OWNED BY "public"."replays"."id";

CREATE TABLE IF NOT EXISTS "public"."servers" (
                                                  "id" bigint NOT NULL,
                                                  "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
                                                  "server_id" "text" NOT NULL,
                                                  "workshop_collection" "text" NOT NULL,
                                                  "hostname" "text" NOT NULL,
                                                  "is_public" boolean NOT NULL,
                                                  "ip" "text" NOT NULL,
                                                  "current_map" "text",
                                                  "real_ip" "text",
                                                  "player_count" integer NOT NULL,
                                                  "total_players" integer NOT NULL,
                                                  "short_name" "text" DEFAULT ''::"text" NOT NULL,
                                                  "players" "jsonb",
                                                  "vip" boolean DEFAULT false NOT NULL,
                                                  "style" "text" DEFAULT 'normal'::"text" NOT NULL
);

ALTER TABLE "public"."servers" OWNER TO "postgres";

ALTER TABLE "public"."servers" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."servers_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
    );

CREATE TABLE IF NOT EXISTS "public"."steam_users" (
                                                      "steamid64" character varying(20) NOT NULL,
                                                      "steamid" character varying(20),
                                                      "communityvisibilitystate" integer,
                                                      "profilestate" integer,
                                                      "personaname" character varying(100),
                                                      "commentpermission" integer,
                                                      "profileurl" character varying(255),
                                                      "avatar" character varying(255),
                                                      "avatarmedium" character varying(255),
                                                      "avatarfull" character varying(255),
                                                      "avatarhash" character varying(50),
                                                      "lastlogoff" bigint,
                                                      "personastate" integer,
                                                      "realname" character varying(100),
                                                      "primaryclanid" character varying(20),
                                                      "timecreated" bigint,
                                                      "personastateflags" integer,
                                                      "gameserverip" character varying(50),
                                                      "gameserversteamid" character varying(20),
                                                      "gameextrainfo" character varying(100),
                                                      "gameid" character varying(10),
                                                      "loccountrycode" character varying(2),
                                                      "locstatecode" character varying(3),
                                                      "loccityid" integer
);

ALTER TABLE "public"."steam_users" OWNER TO "postgres";

CREATE OR REPLACE VIEW "public"."v_ranked_records" AS
SELECT "r2"."id",
       "r2"."steam_id",
       "r2"."mapname",
       "r2"."route",
       "r2"."ticks",
       "r2"."name",
       "r2"."velocity_start_xy",
       "r2"."velocity_start_z",
       "r2"."velocity_start_xyz",
       "r2"."velocity_end_xy",
       "r2"."velocity_end_z",
       "r2"."velocity_end_xyz",
       "r2"."checkpoints",
       "r2"."position",
       "r2"."total_records",
       "r2"."scale_factor",
       "r2"."tier",
       ((10 * "r2"."scale_factor") * "r2"."tier") AS "basic_points",
       "round"(((((100 - ("r2"."tier" * 10)) * "r2"."scale_factor"))::numeric / (("r2"."position")::numeric ^ 0.5))) AS "bonus_points",
       "r2"."style"
FROM ( SELECT "r"."id",
              "r"."steam_id",
              "r"."mapname",
              "r"."route",
              "r"."style",
              "r"."ticks",
              "r"."name",
              "r"."velocity_start_xy",
              "r"."velocity_start_z",
              "r"."velocity_start_xyz",
              "r"."velocity_end_xy",
              "r"."velocity_end_z",
              "r"."velocity_end_xyz",
              "r"."checkpoints",
              COALESCE("rank"() OVER (PARTITION BY "r"."mapname", "r"."route", "r"."style" ORDER BY "r"."ticks"), (0)::bigint) AS "position",
              "count"(*) OVER (PARTITION BY "r"."mapname", "r"."route", "r"."style") AS "total_records",
              CASE
                  WHEN (("r"."route")::"text" = ANY (ARRAY[('main'::character varying)::"text", ('boost'::character varying)::"text"])) THEN 10
                  WHEN (("r"."route")::"text" ~~ 's%'::"text") THEN 1
                  WHEN (("r"."route")::"text" ~~ 'b%'::"text") THEN 3
                  ELSE 1
                  END AS "scale_factor",
              (COALESCE(("m"."route_data" ->> 'Tier'::"text"), '1'::"text"))::integer AS "tier"
       FROM ("public"."records" "r"
           LEFT JOIN "public"."maps_2" "m" ON (((("r"."mapname")::"text" = ("m"."name")::"text") AND (("r"."route")::"text" = ("m"."route")::"text"))))) "r2";

ALTER TABLE "public"."v_ranked_records" OWNER TO "postgres";

CREATE OR REPLACE VIEW "public"."v_player_points" AS
SELECT "r"."id",
       "r"."steam_id",
       "r"."mapname",
       "r"."route",
       "r"."ticks",
       "r"."name",
       "r"."velocity_start_xy",
       "r"."velocity_start_z",
       "r"."velocity_start_xyz",
       "r"."velocity_end_xy",
       "r"."velocity_end_z",
       "r"."velocity_end_xyz",
       "r"."checkpoints",
       "r"."position",
       "r"."total_records",
       "r"."scale_factor",
       "r"."tier",
       "r"."basic_points",
       "r"."bonus_points",
       COALESCE((("r"."basic_points")::numeric + "r"."bonus_points"), (0)::numeric) AS "total_points"
FROM ("public"."players" "p_1"
    LEFT JOIN "public"."v_ranked_records" "r" ON ((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text")));

ALTER TABLE "public"."v_player_points" OWNER TO "postgres";

CREATE OR REPLACE VIEW "public"."v_players" AS
WITH "player_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "total_points"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'normal'::"text"))))
    GROUP BY "p_1"."steam_id"
), "lg_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "lgpoints"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'lg'::"text"))))
    GROUP BY "p_1"."steam_id"
), "tm_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "tmpoints"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'tm'::"text"))))
    GROUP BY "p_1"."steam_id"
), "sw_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "swpoints"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'sw'::"text"))))
    GROUP BY "p_1"."steam_id"
), "hsw_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "hswpoints"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'hsw'::"text"))))
    GROUP BY "p_1"."steam_id"
), "prac_points" AS (
    SELECT "p_1"."steam_id",
           COALESCE("sum"((("r"."basic_points")::numeric + "r"."bonus_points")), (0)::numeric) AS "pracpoints"
    FROM ("public"."players" "p_1"
        LEFT JOIN "public"."v_ranked_records" "r" ON (((("p_1"."steam_id")::"text" = ("r"."steam_id")::"text") AND ("r"."style" = 'prac'::"text"))))
    GROUP BY "p_1"."steam_id"
)
SELECT "p"."id",
       "p"."steam_id",
       "p"."points",
       "p"."is_admin",
       "p"."name",
       "pp"."total_points" AS "p_points",
       "rank"() OVER (ORDER BY "pp"."total_points" DESC) AS "rank",
       "count"(*) OVER () AS "total_players",
       "p"."custom_hud_url",
       "s"."avatarmedium" AS "avatar",
       "s"."loccountrycode" AS "country_code",
       "p"."is_vip",
       "p"."custom_tag",
       "lgp"."lgpoints" AS "lg_points",
       "tmp"."tmpoints" AS "tm_points",
       "rank"() OVER (ORDER BY "lgp"."lgpoints" DESC) AS "lg_rank",
       "rank"() OVER (ORDER BY "tmp"."tmpoints" DESC) AS "tm_rank",
       "swp"."swpoints" AS "sw_points",
       "hswp"."hswpoints" AS "hsw_points",
       "rank"() OVER (ORDER BY "swp"."swpoints" DESC) AS "sw_rank",
       "rank"() OVER (ORDER BY "hswp"."hswpoints" DESC) AS "hsw_rank",
       "pracp"."pracpoints" AS "prac_points",
       "rank"() OVER (ORDER BY "pracp"."pracpoints" DESC) AS "prac_rank",
       "p"."chat_color",
       "p"."name_color"
FROM ((((((("public"."players" "p"
    LEFT JOIN "player_points" "pp" ON ((("p"."steam_id")::"text" = ("pp"."steam_id")::"text")))
    LEFT JOIN "lg_points" "lgp" ON ((("p"."steam_id")::"text" = ("lgp"."steam_id")::"text")))
    LEFT JOIN "tm_points" "tmp" ON ((("p"."steam_id")::"text" = ("tmp"."steam_id")::"text")))
    LEFT JOIN "sw_points" "swp" ON ((("p"."steam_id")::"text" = ("swp"."steam_id")::"text")))
    LEFT JOIN "hsw_points" "hswp" ON ((("p"."steam_id")::"text" = ("hswp"."steam_id")::"text")))
    LEFT JOIN "prac_points" "pracp" ON ((("p"."steam_id")::"text" = ("pracp"."steam_id")::"text")))
    LEFT JOIN "public"."steam_users" "s" ON ((("p"."steam_id")::"text" = ("s"."steamid")::"text")));

ALTER TABLE "public"."v_players" OWNER TO "postgres";

CREATE OR REPLACE VIEW "public"."v_replays" AS
SELECT "r"."id",
       "r"."steam_id",
       "r"."mapname",
       "r"."route",
       "r"."ticks",
       "p"."name" AS "player_name",
       "p"."custom_hud_url",
       "r"."replay_url",
       "rr"."position",
       "rr"."total_records",
       "r"."style"
FROM (("public"."replays" "r"
    LEFT JOIN "public"."players" "p" ON ((("r"."steam_id")::"text" = ("p"."steam_id")::"text")))
    LEFT JOIN "public"."v_ranked_records" "rr" ON (((("r"."steam_id")::"text" = ("rr"."steam_id")::"text") AND (("r"."mapname")::"text" = ("rr"."mapname")::"text") AND (("r"."route")::"text" = ("rr"."route")::"text") AND ("r"."style" = "rr"."style"))));

ALTER TABLE "public"."v_replays" OWNER TO "postgres";

ALTER TABLE ONLY "public"."map_cfg" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."map_cfg_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."maps" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."maps_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."maps_2" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."maps_2_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."players" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."players_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."recent_records" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."recent_records_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."records" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."records_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."replays" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."replays_id_seq"'::"regclass");

ALTER TABLE ONLY "public"."global_cfg"
    ADD CONSTRAINT "global_cfg_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."map_cfg"
    ADD CONSTRAINT "map_cfg_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."maps_2"
    ADD CONSTRAINT "maps_2_name_route_key" UNIQUE ("name", "route");

ALTER TABLE ONLY "public"."maps_2"
    ADD CONSTRAINT "maps_2_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."maps"
    ADD CONSTRAINT "maps_name_key" UNIQUE ("name");

ALTER TABLE ONLY "public"."maps"
    ADD CONSTRAINT "maps_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."players"
    ADD CONSTRAINT "players_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."players"
    ADD CONSTRAINT "players_steam_id_key" UNIQUE ("steam_id");

ALTER TABLE ONLY "public"."recent_records"
    ADD CONSTRAINT "recent_records_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."records"
    ADD CONSTRAINT "records_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."records"
    ADD CONSTRAINT "records_steam_id_mapname_route_style_key" UNIQUE ("steam_id", "mapname", "route", "style");

ALTER TABLE ONLY "public"."replays"
    ADD CONSTRAINT "replays_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."replays"
    ADD CONSTRAINT "replays_steam_id_mapname_route_style_key" UNIQUE ("steam_id", "mapname", "route", "style");

ALTER TABLE ONLY "public"."servers"
    ADD CONSTRAINT "servers_pkey" PRIMARY KEY ("id");

ALTER TABLE ONLY "public"."servers"
    ADD CONSTRAINT "servers_server_id_key" UNIQUE ("server_id");

ALTER TABLE ONLY "public"."steam_users"
    ADD CONSTRAINT "steam_users_pkey" PRIMARY KEY ("steamid64");

CREATE INDEX "idx_records_steamid_mapname" ON "public"."records" USING "btree" ("steam_id", "mapname");

CREATE OR REPLACE TRIGGER "records_insert_update_trigger_before" BEFORE INSERT OR UPDATE ON "public"."records" FOR EACH ROW EXECUTE FUNCTION "public"."insert_into_recent_records_before"();

ALTER TABLE ONLY "public"."records"
    ADD CONSTRAINT "records_steam_id_fkey" FOREIGN KEY ("steam_id") REFERENCES "public"."players"("steam_id");

ALTER TABLE ONLY "public"."replays"
    ADD CONSTRAINT "replays_steam_id_fkey" FOREIGN KEY ("steam_id") REFERENCES "public"."players"("steam_id");

ALTER TABLE "public"."global_cfg" ENABLE ROW LEVEL SECURITY;

ALTER TABLE "public"."servers" ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "public" TO "postgres";
GRANT USAGE ON SCHEMA "public" TO "anon";
GRANT USAGE ON SCHEMA "public" TO "authenticated";
GRANT USAGE ON SCHEMA "public" TO "service_role";

GRANT ALL ON FUNCTION "public"."insert_into_recent_records_before"() TO "anon";
GRANT ALL ON FUNCTION "public"."insert_into_recent_records_before"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."insert_into_recent_records_before"() TO "service_role";

GRANT ALL ON TABLE "public"."global_cfg" TO "anon";
GRANT ALL ON TABLE "public"."global_cfg" TO "authenticated";
GRANT ALL ON TABLE "public"."global_cfg" TO "service_role";

GRANT ALL ON SEQUENCE "public"."global_cfg_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."global_cfg_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."global_cfg_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."map_cfg" TO "anon";
GRANT ALL ON TABLE "public"."map_cfg" TO "authenticated";
GRANT ALL ON TABLE "public"."map_cfg" TO "service_role";

GRANT ALL ON SEQUENCE "public"."map_cfg_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."map_cfg_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."map_cfg_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."maps" TO "anon";
GRANT ALL ON TABLE "public"."maps" TO "authenticated";
GRANT ALL ON TABLE "public"."maps" TO "service_role";

GRANT ALL ON TABLE "public"."maps_2" TO "anon";
GRANT ALL ON TABLE "public"."maps_2" TO "authenticated";
GRANT ALL ON TABLE "public"."maps_2" TO "service_role";

GRANT ALL ON SEQUENCE "public"."maps_2_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."maps_2_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."maps_2_id_seq" TO "service_role";

GRANT ALL ON SEQUENCE "public"."maps_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."maps_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."maps_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."players" TO "anon";
GRANT ALL ON TABLE "public"."players" TO "authenticated";
GRANT ALL ON TABLE "public"."players" TO "service_role";

GRANT ALL ON SEQUENCE "public"."players_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."players_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."players_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."recent_records" TO "anon";
GRANT ALL ON TABLE "public"."recent_records" TO "authenticated";
GRANT ALL ON TABLE "public"."recent_records" TO "service_role";

GRANT ALL ON SEQUENCE "public"."recent_records_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."recent_records_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."recent_records_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."records" TO "anon";
GRANT ALL ON TABLE "public"."records" TO "authenticated";
GRANT ALL ON TABLE "public"."records" TO "service_role";

GRANT ALL ON SEQUENCE "public"."records_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."records_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."records_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."replays" TO "anon";
GRANT ALL ON TABLE "public"."replays" TO "authenticated";
GRANT ALL ON TABLE "public"."replays" TO "service_role";

GRANT ALL ON SEQUENCE "public"."replays_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."replays_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."replays_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."servers" TO "anon";
GRANT ALL ON TABLE "public"."servers" TO "authenticated";
GRANT ALL ON TABLE "public"."servers" TO "service_role";

GRANT ALL ON SEQUENCE "public"."servers_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."servers_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."servers_id_seq" TO "service_role";

GRANT ALL ON TABLE "public"."steam_users" TO "anon";
GRANT ALL ON TABLE "public"."steam_users" TO "authenticated";
GRANT ALL ON TABLE "public"."steam_users" TO "service_role";

GRANT ALL ON TABLE "public"."v_ranked_records" TO "anon";
GRANT ALL ON TABLE "public"."v_ranked_records" TO "authenticated";
GRANT ALL ON TABLE "public"."v_ranked_records" TO "service_role";

GRANT ALL ON TABLE "public"."v_player_points" TO "anon";
GRANT ALL ON TABLE "public"."v_player_points" TO "authenticated";
GRANT ALL ON TABLE "public"."v_player_points" TO "service_role";

GRANT ALL ON TABLE "public"."v_players" TO "anon";
GRANT ALL ON TABLE "public"."v_players" TO "authenticated";
GRANT ALL ON TABLE "public"."v_players" TO "service_role";

GRANT ALL ON TABLE "public"."v_replays" TO "anon";
GRANT ALL ON TABLE "public"."v_replays" TO "authenticated";
GRANT ALL ON TABLE "public"."v_replays" TO "service_role";

ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES  TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES  TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES  TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES  TO "service_role";

ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS  TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS  TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS  TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS  TO "service_role";

ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES  TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES  TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES  TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES  TO "service_role";

RESET ALL;
