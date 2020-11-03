using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.Popups;

namespace PortalMapper
{
    class DataAccess
    {
        public static string DATABASE = "portals.db";
        private static string TABLE = "portal";

        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(DATABASE, CreationCollisionOption.OpenIfExists);
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DATABASE);
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                if(db == null)
                {
                    MainPage.database_ready = false;
                    return;
                }

                String tableCommand = "CREATE TABLE IF NOT EXISTS " +
                    TABLE + " (" +
                    "cell_id LONG PRIMARY KEY, " +
                    "description NVARCHAR(2048), " +
                    "latitude REAL, " +
                    "longitude REAL, " +
                    "flag INTEGER)";

                try
                {
                    db.Open();
                } catch(System.NullReferenceException e)
                {
                    MainPage.database_ready = false;
                    return;
                }

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public static void AddData(PointOfInterest input)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DATABASE);
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand
                {
                    Connection = db,

                    // Use parameterized query to prevent SQL injection attacks
                    CommandText = "INSERT INTO " + TABLE + " VALUES (@CellID, @Description, @Latitude, @Longitude, @Flag);"
                };
                insertCommand.Parameters.AddWithValue("@CellId", input.Leaf);
                insertCommand.Parameters.AddWithValue("@Description", input.DisplayName);
                insertCommand.Parameters.AddWithValue("@Latitude", input.Location.Position.Latitude);
                insertCommand.Parameters.AddWithValue("@Longitude", input.Location.Position.Longitude);
                insertCommand.Parameters.AddWithValue("@Flag", input.Flag);

                insertCommand.ExecuteReader();

                db.Close();
            }
        }
        public static List<PointOfInterest> GetData(string where)
        {
            List<PointOfInterest> list = new List<PointOfInterest>();

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteSample.db");
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    (string.Format("SELECT Text_Entry from {0} WHERE {1}", TABLE, where), db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    PointOfInterest p = new PointOfInterest
                    {
                        Leaf = (ulong)query.GetInt64(0),
                        DisplayName = query.GetString(1),
                        Location = new Geopoint(
                            new BasicGeoposition()
                            {
                                Latitude = query.GetDouble(2),
                                Longitude = query.GetDouble(3)
                            }),
                        Flag = query.GetInt32(4),
                        NormalizedAnchorPoint = MainPage.AnchorPoints[query.GetInt32(4)]
                    };

                    list.Add(p);
                }

                db.Close();
            }

            return list;
        }

    }
}
