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
            USER_ID NUMBER,
            NAME NVARCHAR2(200),
            UNIQUE (USER_ID, NAME)
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

                using (var cmd = conn.CreateCommand()) {
                    //   USERS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE USERS (
            ID NUMBER PRIMARY KEY,
            USERNAME NVARCHAR2(200) UNIQUE,
            PASSWORD_HASH NVARCHAR2(512),
            CREATED_AT TIMESTAMP
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    //  SEQUENCE USERS_SEQ
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE SEQUENCE USERS_SEQ
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
                    //   FAVORITES
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE FAVORITES (
            ID NUMBER PRIMARY KEY,
            USER_ID NUMBER REFERENCES USERS(ID),
            TRACK_ID NUMBER REFERENCES TRACKS(ID),
            CREATED_AT TIMESTAMP,
            UNIQUE (USER_ID, TRACK_ID)
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    //  SEQUENCE FAVORITES_SEQ
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE SEQUENCE FAVORITES_SEQ
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
                    //   PLAYBACK_STATS
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE TABLE PLAYBACK_STATS (
            ID NUMBER PRIMARY KEY,
            USER_ID NUMBER REFERENCES USERS(ID),
            TRACK_ID NUMBER REFERENCES TRACKS(ID),
            PLAYED_AT TIMESTAMP
        )';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    //  SEQUENCE PLAYBACK_STATS_SEQ
                    cmd.CommandText = @"
BEGIN
    EXECUTE IMMEDIATE '
        CREATE SEQUENCE PLAYBACK_STATS_SEQ
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

        public List<PlaylistInfo> LoadPlaylists(int userId) {
            var list = new List<PlaylistInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT ID, NAME FROM PLAYLISTS WHERE USER_ID = :uid ORDER BY ID";
                    cmd.Parameters.Add(new OracleParameter("uid", userId));
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

        public int AddPlaylist(string name, int userId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO PLAYLISTS (ID, USER_ID, NAME) VALUES (PLAYLISTS_SEQ.NEXTVAL, :uid, :name) RETURNING ID INTO :id";
                    cmd.Parameters.Add(new OracleParameter("uid", userId));
                    var nameParam = new OracleParameter("name", OracleDbType.NVarchar2) { Value = name };
                    cmd.Parameters.Add(nameParam);
                    var idParam = new OracleParameter("id", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                    cmd.Parameters.Add(idParam);
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(idParam.Value.ToString());
                }
            }
        }

        public void RenamePlaylist(int playlistId, string newName) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "UPDATE PLAYLISTS SET NAME = :name WHERE ID = :id";
                    cmd.Parameters.Add(new OracleParameter("name", OracleDbType.NVarchar2) { Value = newName });
                    cmd.Parameters.Add(new OracleParameter("id", playlistId));
                    cmd.ExecuteNonQuery();
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
                    var titleParam = new OracleParameter("title", OracleDbType.NVarchar2) { Value = track.Title };
                    cmd.Parameters.Add(titleParam);
                    var pathParam = new OracleParameter("path", OracleDbType.NVarchar2) { Value = track.Path };
                    cmd.Parameters.Add(pathParam);
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

        public void DeletePlaylist(int playlistId, int userId) {
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
                    cmd.CommandText = "DELETE FROM PLAYLISTS WHERE ID = :pid AND USER_ID = :uid";
                    cmd.Parameters.Add(new OracleParameter("pid", playlistId));
                    cmd.Parameters.Add(new OracleParameter("uid", userId));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int RegisterUser(string username, string passwordHash) {
            using (var conn = GetConnection()) {
                conn.Open();
                if (GetUserByUsername(username) != null)
                    throw new InvalidOperationException("USER_EXISTS");
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO USERS (ID, USERNAME, PASSWORD_HASH, CREATED_AT) VALUES (USERS_SEQ.NEXTVAL, :u, :p, :c) RETURNING ID INTO :id";
                    cmd.Parameters.Add(new OracleParameter("u", username));
                    cmd.Parameters.Add(new OracleParameter("p", passwordHash));
                    cmd.Parameters.Add(new OracleParameter("c", DateTime.Now));
                    var idParam = new OracleParameter("id", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                    cmd.Parameters.Add(idParam);
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(idParam.Value.ToString());
                }
            }
        }

        public UserInfo GetUserByUsername(string username) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT ID, USERNAME, PASSWORD_HASH, CREATED_AT FROM USERS WHERE USERNAME = :u";
                    cmd.Parameters.Add(new OracleParameter("u", username));
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            return new UserInfo {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                PasswordHash = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool PlaylistExists(int userId, string name) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT 1 FROM PLAYLISTS WHERE USER_ID = :uid AND NAME = :n";
                    cmd.Parameters.Add(new OracleParameter("uid", userId));
                    cmd.Parameters.Add(new OracleParameter("n", name));
                    using var reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }

        public void AddFavorite(int userId, int trackId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO FAVORITES (ID, USER_ID, TRACK_ID, CREATED_AT) VALUES (FAVORITES_SEQ.NEXTVAL, :u, :t, :c)";
                    cmd.Parameters.Add(new OracleParameter("u", userId));
                    cmd.Parameters.Add(new OracleParameter("t", trackId));
                    cmd.Parameters.Add(new OracleParameter("c", DateTime.Now));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RemoveFavorite(int userId, int trackId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "DELETE FROM FAVORITES WHERE USER_ID = :u AND TRACK_ID = :t";
                    cmd.Parameters.Add(new OracleParameter("u", userId));
                    cmd.Parameters.Add(new OracleParameter("t", trackId));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TrackInfo> GetUserFavorites(int userId) {
            var list = new List<TrackInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"SELECT t.ID, t.TITLE, t.PATH, t.IS_YOUTUBE, t.CREATED_AT FROM TRACKS t JOIN FAVORITES f ON t.ID = f.TRACK_ID WHERE f.USER_ID = :u ORDER BY f.CREATED_AT DESC";
                    cmd.Parameters.Add(new OracleParameter("u", userId));
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

        public void RecordPlayback(int userId, int trackId) {
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"INSERT INTO PLAYBACK_STATS (ID, USER_ID, TRACK_ID, PLAYED_AT) VALUES (PLAYBACK_STATS_SEQ.NEXTVAL, :u, :t, :c)";
                    cmd.Parameters.Add(new OracleParameter("u", userId));
                    cmd.Parameters.Add(new OracleParameter("t", trackId));
                    cmd.Parameters.Add(new OracleParameter("c", DateTime.Now));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TrackStatInfo> GetTopPlayedTracks(int topN) {
            var list = new List<TrackStatInfo>();
            using (var conn = GetConnection()) {
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"SELECT * FROM (
                        SELECT t.ID, t.TITLE, t.PATH, t.IS_YOUTUBE, t.CREATED_AT, COUNT(*) AS C
                        FROM TRACKS t JOIN PLAYBACK_STATS p ON t.ID = p.TRACK_ID
                        GROUP BY t.ID, t.TITLE, t.PATH, t.IS_YOUTUBE, t.CREATED_AT
                        ORDER BY C DESC)
                        WHERE ROWNUM <= :n";
                    cmd.Parameters.Add(new OracleParameter("n", topN));
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            list.Add(new TrackStatInfo {
                                Track = new TrackInfo {
                                    Id = reader.GetInt32(0),
                                    Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    Path = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    IsYouTube = reader.GetInt32(3) == 1,
                                    CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                                },
                                PlayCount = reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}

