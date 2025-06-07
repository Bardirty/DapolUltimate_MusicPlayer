using System;
using System.Collections.Generic;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace DapolUltimate_MusicPlayer {
    public class OracleDbService {
        private readonly string _connectionString;

        public OracleDbService() {
            _connectionString = ConfigurationManager.ConnectionStrings["OracleDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(_connectionString) || _connectionString.Contains("{ORACLE_CONNECTION_STRING}")) {
                var env = Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING");
                if (string.IsNullOrWhiteSpace(env))
                    throw new InvalidOperationException("Oracle connection string not configured. Set ORACLE_CONNECTION_STRING environment variable.");
                _connectionString = env;
            }
        }

        private OracleConnection GetConnection() => new OracleConnection(_connectionString);

        public void EnsureTableExists() {
            using (var conn = GetConnection()) {
                conn.Open();

                using (var cmd = conn.CreateCommand()) {
                    // Ñîçäàíèå òàáëèöû TRACKS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE TRACKS (
            ID NUMBER PRIMARY KEY,
            TITLE VARCHAR2(400),
            PATH VARCHAR2(1024),
            IS_YOUTUBE NUMBER(1),
            CREATED_AT TIMESTAMP
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    // Ñîçäàíèå SEQUENCE TRACKS_SEQ
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE SEQUENCE TRACKS_SEQ
        START WITH 1
        INCREMENT BY 1
        NOMAXVALUE
        NOCACHE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public List<TrackInfo> LoadTracks() {
            var list = new List<TrackInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT ID, TITLE, PATH, IS_YOUTUBE, CREATED_AT FROM TRACKS ORDER BY ID";
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            list.Add(new TrackInfo {
                                Id = reader.GetInt32(0),
                                Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Path = reader.IsDBNull(2) ? null : reader.GetString(2),
                                IsYouTube = reader.GetInt32(3) == 1,
                                CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public int AddTrack(TrackInfo track) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO TRACKS (ID, TITLE, PATH, IS_YOUTUBE, CREATED_AT)
VALUES (TRACKS_SEQ.NEXTVAL, :title, :path, :isYT, :created) RETURNING ID INTO :id";

                    cmd.Parameters.Add(new OracleParameter("title", track.Title));
                    cmd.Parameters.Add(new OracleParameter("path", track.Path));
                    cmd.Parameters.Add(new OracleParameter("isYT", track.IsYouTube ? 1 : 0));
                    cmd.Parameters.Add(new OracleParameter("created", track.CreatedAt));
                    var idParam = new OracleParameter("id", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                    cmd.Parameters.Add(idParam);
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(idParam.Value.ToString());
                }
            }
        }

        public void DeleteTrack(int id) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM TRACKS WHERE ID = :id";
                    cmd.Parameters.Add(new OracleParameter("id", id));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
