using System.Collections.Generic;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Google.Common.Geometry;
using System;
using Windows.UI.Popups;
using System.IO;
using Newtonsoft.Json;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using System.Text;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PortalMapper
{
    public sealed partial class MainPage : Page
    {
        public static bool database_ready = true;

        MapElementsLayer gymsLayer;
        MapElementsLayer stopsLayer;
        MapElementsLayer portalsLayer;
        IList<MapLayer> myLayers = new ObservableCollection<MapLayer>();

        static string[] tags = { "Arena", "Pokéstop", "Portal" };

        static Geopoint HoexterGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 51.774162, Longitude = 9.3827203 });
        static Geopoint HolzmindenGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 51.828343, Longitude = 9.4415303 });
        static Geopoint GoslarGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 51.911584, Longitude = 10.4277413 });
        static Geopoint BraunschweigGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 52.262026, Longitude = 10.5187523 });

        public static Windows.Foundation.Point[] AnchorPoints = { 
            new Windows.Foundation.Point(0.5, 0.8), 
            new Windows.Foundation.Point(0.5, 0.5), 
            new Windows.Foundation.Point(0.5, 0.5) 
        };

        List<MapPolygon> lvl14Cells;
        List<MapPolygon> lvl17Cells;

        List<PointOfInterest> portals;

        RandomAccessStreamReference portalIcon;
        RandomAccessStreamReference portalHoverIcon;
        RandomAccessStreamReference stopIcon;
        RandomAccessStreamReference stopHoverIcon;
        RandomAccessStreamReference gymIcon;
        RandomAccessStreamReference gymHoverIcon;

        S2RegionCoverer lvl17Coverer;
        S2RegionCoverer lvl14Coverer;

        MapPolygon highlight;

        private async void CheckDatabase()
        {
            if (!database_ready)
            {
                var messageDialog = new MessageDialog("Datenbank konnte nicht geladen werden.");
                messageDialog.Commands.Add(new UICommand("OK", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.DefaultCommandIndex = 0;
                messageDialog.Title = "Fehler";
                await messageDialog.ShowAsync();
            }
        }

        public MainPage()
        {
            InitializeComponent();

            CheckDatabase();

            portals = new List<PointOfInterest>();
            lvl14Cells = new List<MapPolygon>();
            lvl17Cells = new List<MapPolygon>();

            portalIcon = null;
            portalHoverIcon = null;
            stopIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pokedisk_blue_36.png"));
            stopHoverIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pokedisk_black_36.png"));
            gymIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/gym_red_48.png"));
            gymHoverIcon = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/gym_black_48.png"));

            lvl17Coverer = new S2RegionCoverer
            {
                MaxLevel = 17,
                MinLevel = 17,
                MaxCells = 1
            };

            lvl14Coverer = new S2RegionCoverer
            {
                MaxLevel = 14,
                MinLevel = 14
            };

        }

        private void PokeMap_Loaded(object sender, RoutedEventArgs e)
        {
            PokeMap.Center = HoexterGeopoint;
            PokeMap.ZoomLevel = 13;

            AddLayers();

            PokeMap.Layers = myLayers;
        }

        private void AddLayers()
        {
            gymsLayer = new MapElementsLayer();
            stopsLayer = new MapElementsLayer();
            portalsLayer = new MapElementsLayer();
            myLayers.Clear();

            var listOfGyms = portals.Where(p => p.Flag == PointOfInterest.GYM);
            var listOfStops = portals.Where(p => p.Flag == PointOfInterest.STOP);
            var listOfPortals = portals.Where(p => p.Flag == PointOfInterest.PORTAL);

            foreach (var item in listOfGyms)
            {
                gymsLayer.MapElements.Add(new MapIcon()
                {
                    Location = item.Location,
                    Image = gymIcon,
                    Title = item.DisplayName,
                    NormalizedAnchorPoint = AnchorPoints[item.Flag],
                    Visible = true,
                    Tag = tags[item.Flag]
                });
            }

            foreach (var item in listOfStops)
            {
                gymsLayer.MapElements.Add(new MapIcon()
                {
                    Location = item.Location,
                    Image = stopIcon,
                    Title = item.DisplayName,
                    NormalizedAnchorPoint = AnchorPoints[item.Flag],
                    Visible = true,
                    Tag = tags[item.Flag]
                });
            }

            foreach (var item in listOfPortals)
            {
                gymsLayer.MapElements.Add(new MapIcon()
                {
                    Location = item.Location,
                    Title = item.DisplayName,
                    NormalizedAnchorPoint = AnchorPoints[item.Flag],
                    Visible = true,
                    Tag = tags[item.Flag]
                });
            }

            gymsLayer.MapElementPointerEntered += (sender, args) => UpdateMapElementOnPointerEntered(args.MapElement);
            gymsLayer.MapElementPointerExited += (sender, args) => UpdateMapElementOnPointerExited(args.MapElement);
            gymsLayer.MapElementClick += (sender, args) => UpdateMapElementOnClick(args.MapElements[0]);
            gymsLayer.MapContextRequested += (sender, args) => ShowMapElementContextMenu(args.MapElements[0]);

            stopsLayer.MapElementPointerEntered += (sender, args) => UpdateMapElementOnPointerEntered(args.MapElement);
            stopsLayer.MapElementPointerExited += (sender, args) => UpdateMapElementOnPointerExited(args.MapElement);
            stopsLayer.MapElementClick += (sender, args) => UpdateMapElementOnClick(args.MapElements[0]);
            stopsLayer.MapContextRequested += (sender, args) => ShowMapElementContextMenu(args.MapElements[0]);

            portalsLayer.MapElementPointerEntered += (sender, args) => UpdateMapElementOnPointerEntered(args.MapElement);
            portalsLayer.MapElementPointerExited += (sender, args) => UpdateMapElementOnPointerExited(args.MapElement);
            portalsLayer.MapElementClick += (sender, args) => UpdateMapElementOnClick(args.MapElements[0]);
            portalsLayer.MapContextRequested += (sender, args) => ShowMapElementContextMenu(args.MapElements[0]);

            myLayers.Add(gymsLayer);
            myLayers.Add(stopsLayer);
            myLayers.Add(portalsLayer);
        }

        void UpdateMapElementOnPointerEntered(MapElement mapElement)
        {
            mapElement.MapStyleSheetEntryState = MapStyleSheetEntryStates.Hover;
            if((string)mapElement.Tag == tags[0])
            {
                ((MapIcon)mapElement).Image = gymHoverIcon;
            }
            if ((string)mapElement.Tag == tags[1])
            {
                ((MapIcon)mapElement).Image = stopHoverIcon;
            }
        }

        void UpdateMapElementOnPointerExited(MapElement mapElement)
        {
            if(mapElement.MapStyleSheetEntryState != MapStyleSheetEntryStates.Selected)
            {
                mapElement.MapStyleSheetEntryState = "";
            }
            if ((string)mapElement.Tag == tags[0])
            {
                ((MapIcon)mapElement).Image = gymIcon;
            }
            if ((string)mapElement.Tag == tags[1])
            {
                ((MapIcon)mapElement).Image = stopIcon;
            }
        }

        static void UpdateMapElementOnClick(MapElement mapElement)
        {
            mapElement.MapStyleSheetEntryState = MapStyleSheetEntryStates.Selected;
        }

        async void ShowMapElementContextMenu(MapElement mapElement)
        {
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(tags[0], (command) =>
            {
                mapElement.Tag = tags[0];
                ((MapIcon)mapElement).Image = gymIcon;
                ((MapIcon)mapElement).NormalizedAnchorPoint = AnchorPoints[0];
            }));
            menu.Commands.Add(new UICommand(tags[1], (command) =>
            {
                mapElement.Tag = tags[1];
                ((MapIcon)mapElement).Image = stopIcon;
                ((MapIcon)mapElement).NormalizedAnchorPoint = AnchorPoints[1];
            }));
            menu.Commands.Add(new UICommand(tags[2], (command) =>
            {
                mapElement.Tag = tags[2];
                ((MapIcon)mapElement).Image = portalIcon;
                ((MapIcon)mapElement).NormalizedAnchorPoint = AnchorPoints[2];
            }));
            var pointOnMap = new Windows.Foundation.Point();
            PokeMap.GetOffsetFromLocation(((MapIcon)mapElement).Location, out pointOnMap);
            double x = pointOnMap.X;
            double y = pointOnMap.Y;
            double width = 100;
            double height = 50 * tags.Length;

            if(Window.Current.Bounds.Width < x + width)
            {
                x -= width;
            }
            if (Window.Current.Bounds.Height < y + height)
            {
                y -= height;
            }

            await menu.ShowForSelectionAsync(new Windows.Foundation.Rect(x, y, width, height));
        }

        async void Load_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".geojson");
            picker.FileTypeFilter.Add(".geoJSON");
            picker.FileTypeFilter.Add(".csv");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(file.Path))
                    {
                        string content = sr.ReadToEnd();
                        if (file.FileType == ".csv")
                        {
                            string[] lines = content.Split('\n');
                            for (int i = 0; i < lines.Length; i++)
                            {
                                var item = lines[i];

                                if (String.IsNullOrWhiteSpace(item)) continue;

                                string[] info = item.Split(',');
                                double lat = 0;
                                double lon = 0;

                                if (!Double.TryParse(info[info.Length - 2], out lon) || !Double.TryParse(info[info.Length - 3], out lat))
                                {
                                    if (i == 0) continue;
                                    var messageDialog = new MessageDialog("Zeile " + i + " (" + item + ") hat nicht das richtige Format!");
                                    messageDialog.Commands.Add(new UICommand("OK", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                                    messageDialog.DefaultCommandIndex = 0;
                                    messageDialog.Title = "Fehlerhafte Datei";
                                    await messageDialog.ShowAsync();
                                    return;
                                }

                                StringBuilder sb = new StringBuilder("");

                                for (int j = 0; j < info.Length - 3; j++)
                                {
                                    sb.Append(info[j]);
                                }

                                PointOfInterest poi = new PointOfInterest()
                                {
                                    Location = new Geopoint(new BasicGeoposition() { Latitude = lat, Longitude = lon }),
                                    DisplayName = sb.ToString(),
                                    NormalizedAnchorPoint = AnchorPoints[2],
                                    Flag = PointOfInterest.PORTAL,
                                    Leaf = FindExactCell(new BasicGeoposition() { Latitude = lat, Longitude = lon }, 30).Id.Id
                                };

                                portals.Add(poi);
                                //DataAccess.AddData(poi);
                            }
                        }
                        else
                        {
                            var features = JsonConvert.DeserializeObject<FeatureCollection>(content).Features;

                            foreach (var p in features)
                            {
                                double lon = ((Point)p.Geometry).Coordinates.Longitude;
                                double lat = ((Point)p.Geometry).Coordinates.Latitude;
                                int flag = PointOfInterest.PORTAL;
                                if (p.Properties.ContainsKey("Flag"))
                                {
                                    flag = (int)p.Properties["Flag"]; 
                                }

                                PointOfInterest poi = new PointOfInterest()
                                {
                                    Location = new Geopoint(new BasicGeoposition() { Latitude = lat, Longitude = lon }),
                                    DisplayName = (string)p.Properties["name"],
                                    NormalizedAnchorPoint = AnchorPoints[flag],
                                    Flag = flag,
                                    Leaf = FindExactCell(new BasicGeoposition() { Latitude = lat, Longitude = lon }, 30).Id.Id
                                };

                                portals.Add(poi);
                                //DataAccess.AddData(poi);
                            }
                        }
                        AddLayers();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                    var messageDialog = new MessageDialog("Zugriff auf den Pfad " + file.Path +
                        " wurde verweigert.\nBitte gewähren Sie Zugriff unter \n\nEinstellungen -> Privatsphäre -> Dateisystem\n\noder bewegen Sie die Datei an diesen Ort:\n"
                        + installedLocation.Path);
                    messageDialog.Commands.Add(new UICommand("OK", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                    messageDialog.DefaultCommandIndex = 0;
                    messageDialog.Title = "Keine Zugriffsrechte";
                    await messageDialog.ShowAsync();
                    return;
                }

                // outermost bounds
                double maxLat = -360;
                double maxLon = -360;
                double minLat = 360;
                double minLon = 360;

                // Find out all Lvl17 cells
                foreach (var p in portals)
                {
                    double lat = p.Location.Position.Latitude;
                    double lon = p.Location.Position.Longitude;

                    if (lon < minLon) minLon = lon;
                    if (lon > maxLon) maxLon = lon;
                    if (lat < minLat) minLat = lat;
                    if (lat > maxLat) maxLat = lat;

                    var cell = FindExactCell(p.Location.Position, 17);

                    MapPolygon polygon = new MapPolygon
                    {
                        Path = new Geopath(new List<BasicGeoposition>()
                        {
                            FromS2Point(cell.GetVertex(0)),
                            FromS2Point(cell.GetVertex(1)),
                            FromS2Point(cell.GetVertex(2)),
                            FromS2Point(cell.GetVertex(3))
                        }),

                        ZIndex = 1,
                        FillColor = Color.FromArgb(50, 50, 50, 50),
                        StrokeColor = Colors.Black,
                        StrokeThickness = 2,
                        StrokeDashed = false

                    };
                    lvl17Cells.Add(polygon);
                    AddCell(polygon);
                }

                // Find out Lvl14 cells
                var area = S2LatLngRect.FromPointPair(
                        S2LatLng.FromDegrees(minLat, minLon),
                        S2LatLng.FromDegrees(maxLat, maxLon)
                    );

                var cells14 = new List<S2CellId>();
                lvl14Coverer.GetCovering(area, cells14);

                for (int i = 0; i < cells14.Count; i++)
                {
                    S2Cell cell = new S2Cell(cells14[i]);

                    MapPolygon polygon = new MapPolygon
                    {
                        Path = new Geopath(new List<BasicGeoposition>()
                    {
                        FromS2Point(cell.GetVertex(0)),
                        FromS2Point(cell.GetVertex(1)),
                        FromS2Point(cell.GetVertex(2)),
                        FromS2Point(cell.GetVertex(3))
                    }),

                        ZIndex = 1,
                        FillColor = Color.FromArgb(50, 0, 0, 50),
                        StrokeColor = Colors.Blue,
                        StrokeThickness = 2,
                        StrokeDashed = false

                    };
                    lvl14Cells.Add(polygon);
                    AddCell(polygon);
                }

                BasicGeoposition newCenter = new BasicGeoposition();
                newCenter.Latitude = (minLat + maxLat) / 2;
                newCenter.Longitude = (minLon + maxLon) / 2;
                PokeMap.Center = new Geopoint(newCenter);
            }
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            PokeMap.MapElements.Clear();
        }

        public void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        public void Map_Tap(object sender, MapInputEventArgs args)
        {
            HighlighCell(args.Location.Position, 14);
        }

        public void ZoomLevelChanged(MapControl sender, object args)
        {
            PoiInfo.Text = PokeMap.ZoomLevel.ToString();
        }

        public void CenterChanged(MapControl sender, object args)
        {

        }

        private void mapItemButton_Click(object sender, RoutedEventArgs e)
        {
            var buttonSender = sender as Button;
            PointOfInterest poi = buttonSender.DataContext as PointOfInterest;
            CommandBarInfo.Text = poi.DisplayName;

            HighlighCell(poi.Location.Position, 17);
        }

        public void HighlighCell(BasicGeoposition pos, int level)
        {
            PokeMap.MapElements.Remove(highlight);

            var cell = FindExactCell(pos, level);

            highlight = new MapPolygon
            {
                Path = new Geopath(new List<BasicGeoposition>()
                        {
                            FromS2Point(cell.GetVertex(0)),
                            FromS2Point(cell.GetVertex(1)),
                            FromS2Point(cell.GetVertex(2)),
                            FromS2Point(cell.GetVertex(3))
                        }),

                ZIndex = 1,
                FillColor = Color.FromArgb(50, 50, 0, 0),
                StrokeColor = Colors.Red,
                StrokeThickness = 2,
                StrokeDashed = false

            };
            AddCell(highlight);
        }

        /// <summary>
        /// Find the cell of the given level that covers the given Geoposition. 
        /// This method uses the library given S2RegionCoverer to find the leaf cell containing the given Geoposition
        /// and uses binary operation on the leaf cell's Id to find the higher level cell containing the leaf.
        /// This is somehow more accurate than using the S2RegionCoverer for the higher level cell, where some errors occured.
        /// </summary>
        /// <param name="pos">Position in the cell</param>
        /// <param name="level">Level of the cell</param>
        /// <returns>The cell covering the given position that matches the specifications</returns>
        private S2Cell FindExactCell(BasicGeoposition pos, int level)
        {
            var point = S2LatLngRect.FromPoint(S2LatLng.FromDegrees(pos.Latitude, pos.Longitude));

            var cells = new List<S2CellId>();

            // find leaf cell
            var coverer = new S2RegionCoverer()
            {
                MinLevel = 30,
                MaxLevel = 30,
                MaxCells = 1
            };
            coverer.GetCovering(point, cells);

            var leaf = new S2Cell(cells[0]);

            int shift = 64 - (3 + level * 2 + 1);
            ulong id = leaf.Id.Id & ulong.MaxValue << shift;
            id |= 0 | ((ulong)1 << shift);

            return new S2Cell(new S2CellId(id));
        }

        public void AddCell(MapPolygon cell)
        {
            PokeMap.MapElements.Add(cell);
        }

        private BasicGeoposition FromS2Point(S2Point point)
        {
            var newLatLng = new S2LatLng(point);
            return new BasicGeoposition()
            {
                Latitude = newLatLng.LatDegrees,
                Longitude = newLatLng.LngDegrees
            };
        }

        private List<PointOfInterest> GetVisibleItems(Geopath visible)
        {
            if (PokeMap.ZoomLevel < 15) return null;

            var visibleItems = new List<PointOfInterest>();

            foreach (var p in portals)
            {
                if (PointInPolygon(p.Location.Position, visible))
                {
                    visibleItems.Add(p);
                }
            }

            return visibleItems;
        }

        // Credit
        // http://csharphelper.com/blog/2014/07/determine-whether-a-point-is-inside-a-polygon-in-c/

        // Return True if the point is in the polygon.
        public static bool PointInPolygon(BasicGeoposition point, Geopath polygon)
        {
            double X = point.Longitude;
            double Y = point.Latitude;

            List<BasicGeoposition> points = new List<BasicGeoposition>(polygon.Positions);
            // Get the angle between the point and the
            // first and last vertices.
            double total_angle = GetAngle(
                points[3].Longitude, points[3].Latitude,
                X, Y,
                points[0].Longitude, points[0].Latitude);

            // Add the angles from the point
            // to each other pair of vertices.
            for (int i = 0; i < 3; i++)
            {
                total_angle += GetAngle(
                    points[i].Longitude, points[i].Latitude,
                    X, Y,
                    points[i + 1].Longitude, points[i + 1].Latitude);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            // The following statement was changed. See the comments.
            //return (Math.Abs(total_angle) > 0.000001);
            return (Math.Abs(total_angle) > 1);
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        public static double GetAngle(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the dot product.
            double dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

            // Get the cross product.
            double cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return Math.Atan2(cross_product, dot_product);
        }

        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        public static double CrossProductLength(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        private static double DotProduct(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the dot product.
            return (BAx * BCx + BAy * BCy);
        }

        private void CommandInvokedHandler(IUICommand command)
        {

        }
    }

    public class PointOfInterest
    {
        public ulong Leaf { get; set; }
        public int Flag { get; set; }
        public string DisplayName { get; set; }
        public Geopoint Location { get; set; }
        public Windows.Foundation.Point NormalizedAnchorPoint { get; set; }

        public static int PORTAL = 2;
        public static int STOP = 1;
        public static int GYM = 0;
    }
}
