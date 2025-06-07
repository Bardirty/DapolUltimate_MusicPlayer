using System;
using System.Collections.Generic;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace DapolUltimate_MusicPlayer {
    public class OracleDbService {
        private readonly string _connectionString;

        public OracleDbService() {
            _connectionString = ConfigurationManager.ConnectionStrings["OracleDb"]?.ConnectionString;
        }

        private OracleConnection GetConnection() => new OracleConnection(_connectionString);

        public void EnsureTableExists() {
            using (var conn = GetConnection()) {
                conn.Open();

                using (var cmd = conn.CreateCommand()) {
                    //   TRACKS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE TRACKS (
            ID NUMBER PRIMARY KEY,
            TITLE NVARCHAR2(400),
            PATH NVARCHAR2(1024),
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
                    //  SEQUENCE TRACKS_SEQ
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

                using (var cmd = conn.CreateCommand()) {
                    //   PLAYLISTS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE PLAYLISTS (
            ID NUMBER PRIMARY KEY,
            NAME NVARCHAR2(200)
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    //  SEQUENCE PLAYLISTS_SEQ
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE SEQUENCE PLAYLISTS_SEQ
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

                using (var cmd = conn.CreateCommand()) {
                    //   PLAYLIST_TRACKS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE PLAYLIST_TRACKS (
            PLAYLIST_ID NUMBER,
            TRACK_ID NUMBER,
            PRIMARY KEY (PLAYLIST_ID, TRACK_ID)
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<PlaylistInfo> LoadPlaylists() {
            var list = new List<PlaylistInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT ID, NAME FROM PLAYLISTS ORDER BY ID";
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            list.Add(new PlaylistInfo {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public int AddPlaylist(string name) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO PLAYLISTS (ID, NAME) VALUES (PLAYLISTS_SEQ.NEXTVAL, :name) RETURNING ID INTO :id";
                    cmd.Parameters.Add(new OracleParameter("name", name));
                    var idParam = new OracleParameter("id", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                    cmd.Parameters.Add(idParam);
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(idParam.Value.ToString());
                }
            }
        }

        public List<TrackInfo> LoadPlaylistTracks(int playlistId) {
            var list = new List<TrackInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"SELECT t.ID, t.TITLE, t.PATH, t.IS_YOUTUBE, t.CREATED_AT FROM TRACKS t JOIN PLAYLIST_TRACKS p ON t.ID = p.TRACK_ID WHERE p.PLAYLIST_ID = :pid ORDER BY t.ID";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
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

        public void AddTrackToPlaylist(int playlistId, int trackId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "INSERT INTO PLAYLIST_TRACKS (PLAYLIST_ID, TRACK_ID) VALUES (:pid, :tid)";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.Parameters.Add(new OracleParameter("tid", trackId));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int AddTrack(TrackInfo track) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO TRACKS (ID, TITLE, PATH, IS_YOUTUBE, CREATED_AT) VALUES (TRACKS_SEQ.NEXTVAL, :title, :path, :isYT, :created) RETURNING ID INTO :id";
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

        public void DeleteTrack(int playlistId, int trackId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM PLAYLIST_TRACKS WHERE PLAYLIST_ID = :pid AND TRACK_ID = :tid";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.Parameters.Add(new OracleParameter("tid", trackId));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM TRACKS WHERE ID = :id";
                    cmd.Parameters.Add(new OracleParameter("id", trackId));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeletePlaylist(int playlistId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText =
                        "DELETE FROM TRACKS WHERE ID IN (SELECT TRACK_ID FROM PLAYLIST_TRACKS WHERE PLAYLIST_ID = :pid)";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM PLAYLIST_TRACKS WHERE PLAYLIST_ID = :pid";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM PLAYLISTS WHERE ID = :pid";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

