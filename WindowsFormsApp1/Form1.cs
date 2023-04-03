using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;


namespace WindowsFormsApp1
{
    public class Unit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        public Unit(int id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
        }
    }

    public class FuctionUnits
    {
        private SqlConnection connection;

        public void openConnectionDB(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к БД!");
            }
        }

        public bool isConnectionOpen()
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
                return true;
            else
                return false;
        }

        public void closeConnectionDB()
        {
            connection.Close();
        }

        public List<Unit> CreateUnits()
        {
            List<Unit> units = new List<Unit>();

            if (isConnectionOpen())
            {
                string query = "SELECT * FROM units";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    units.Add(new Unit(Convert.ToInt32(reader["id"]), Convert.ToString(reader["name"]),
                        Convert.ToDouble(reader["lat"]), Convert.ToDouble(reader["lon"])
                        ));
                }
                reader.Close();
            }
            return units;
        }

        public void changePosition(GMapMarker ClickMarker, PointLatLng pointClickMarker)
        {
            if (isConnectionOpen())
            {
                string[] id = ClickMarker.ToolTipText.Split('.');
                string query = "UPDATE units SET lat=" + Convert.ToString(pointClickMarker.Lat).Replace(',', '.')
                        + ", lon=" + Convert.ToString(pointClickMarker.Lng).Replace(',', '.') + " WHERE id=" + id[0];
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
    }

    public partial class Form1 : Form
    {
        
        GMapMarker ClickMarker = null;
        PointLatLng pointClickMarker;
        Point MouseDownPoint;
        bool isMouseDown = false;

        FuctionUnits units = new FuctionUnits();

        public Form1()
        {
            InitializeComponent();
        }

        public GMapOverlay CreateMarkers(List<Unit> listUnits)
        {
            GMapOverlay markers = new GMapOverlay("markers");
            foreach (var unit in listUnits) {
                GMapMarker marker = new GMarkerGoogle(
                                    new PointLatLng(unit.Lat, unit.Lon),
                                    GMarkerGoogleType.orange);
                marker.ToolTipText = Convert.ToString(unit.Id) + ". " + unit.Name;
                markers.Markers.Add(marker);
            }
           
            return markers;
        }

        public GMapPolygon CreatePolygon()
        {
            List<PointLatLng> points = new List<PointLatLng>();
            points.Add(new PointLatLng(55.023999, 82.929444));
            points.Add(new PointLatLng(55.044738, 82.955660));
            points.Add(new PointLatLng(55.029951, 82.995756));
            points.Add(new PointLatLng(55.008025, 82.962960));
            GMapPolygon polygon = new GMapPolygon(points, "Polygon");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
            polygon.Stroke = new Pen(Color.Red, 1);

            return polygon;
        }

        public double[] parseMessageNMEA(string messageNMEA)
        {
            double lat_grad, lon_grad, lat_min, lon_min, lat_full, lon_full;
            string[] listMessagesGPGGA;
            double[] coordinates = new double[2];
            IFormatProvider format = new NumberFormatInfo { NumberDecimalSeparator = "." };
            listMessagesGPGGA = messageNMEA.Split(',');

            lat_grad = double.Parse(listMessagesGPGGA[2].Substring(0, 2));
            lat_min = double.Parse(listMessagesGPGGA[2].Substring(2, listMessagesGPGGA[2].Length - 3), format);

            lon_grad = double.Parse(listMessagesGPGGA[4].Substring(0, 3));
            lon_min = double.Parse(listMessagesGPGGA[4].Substring(3, listMessagesGPGGA[4].Length - 4), format);

            lat_full = lat_grad + Math.Round(lat_min / 60, 6);
            lon_full = lon_grad + Math.Round(lon_min / 60, 6);

            coordinates[0] = lat_full;
            coordinates[1] = lon_full;

            return coordinates;
        }

        public void ShowDialogWindow()
        {
            Form dlg = new Form();
            dlg.Text = "Диалоговое окно";
            Label label = new Label();
            label.Location = new Point(dlg.Size.Height / 2 - 30, dlg.Size.Width / 2 - 40);
            label.Text = "Маркер внутри области!";
            dlg.Controls.Add(label);
            dlg.ShowDialog();
        }

        private void gMapControl1_Load(object sender, EventArgs e)
        {
            gMapControl1.MapProvider = GoogleMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            gMapControl1.MinZoom = 2; 
            gMapControl1.MaxZoom = 16; 
            gMapControl1.Zoom = 13; 
            gMapControl1.Position = new GMap.NET.PointLatLng(55.030204, 82.92043);
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter; 
            gMapControl1.CanDragMap = true; 
            gMapControl1.DragButton = MouseButtons.Right; 
            gMapControl1.ShowCenter = false; 
            gMapControl1.ShowTileGridLines = false;
            
            GMapOverlay polygons = new GMapOverlay("polygons");
            string connectionString = @"Data Source=LAPTOP-S6SIUI0Q; Initial Catalog=dbunits;Integrated Security=True";
            units.openConnectionDB(connectionString);

            polygons.Polygons.Add(CreatePolygon());
            gMapControl1.Overlays.Add(polygons);
            gMapControl1.Overlays.Add(CreateMarkers(units.CreateUnits()));
        }

        private void gMapControl1_OnMarkerEnter(GMapMarker item)
        {
            if(ClickMarker == null)
                ClickMarker = item;
        }

        private void gMapControl1_OnMarkerLeave(GMapMarker item)
        {
            if (!isMouseDown)
                ClickMarker = null;
        }

        private void gMapControl1_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            MouseDownPoint = new Point(e.Location.X, e.Location.Y);
            
        }

        private void gMapControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (ClickMarker != null && isMouseDown && e.Button == MouseButtons.Left)
            {
                pointClickMarker = gMapControl1.FromLocalToLatLng(e.Location.X, e.Location.Y);
                ClickMarker.Position = new PointLatLng(pointClickMarker.Lat, pointClickMarker.Lng);
            }
        }

        private void gMapControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (ClickMarker != null)
            {
                isMouseDown = false;
                units.changePosition(ClickMarker, pointClickMarker);

                if (gMapControl1.Overlays[0].Polygons[0].IsInside(ClickMarker.Position))
                {
                    ShowDialogWindow();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            units.closeConnectionDB();
        }

        private void Wait(double seconds)
        {
            int ticks = System.Environment.TickCount + (int)Math.Round(seconds * 1000.0);
            while (System.Environment.TickCount < ticks)
            {
                Application.DoEvents();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int i = 0;
            string messageNMEA;
            double[] coordinatesParse;
            StreamReader f = new StreamReader(@"\taskB\output.nmea");

            while ((messageNMEA = f.ReadLine()) != null)
            {
                if (messageNMEA.IndexOf("$GPGGA") != -1)
                {  
                    coordinatesParse = parseMessageNMEA(messageNMEA);
                    gMapControl1.Overlays[1].Markers[gMapControl1.Overlays[1].Markers.Count - 1].Position = new PointLatLng(coordinatesParse[0], coordinatesParse[1]);
                    if (gMapControl1.Overlays[0].Polygons[0].IsInside(gMapControl1.Overlays[1].Markers[gMapControl1.Overlays[1].Markers.Count - 1].Position) && i<1)
                    {
                        i++;
                        ShowDialogWindow();
                    }
                    Wait(0.1);
                    label1.Text = "Маркер движется";
                }
            }

            label1.Text = "Маркер остановился";
            f.Close();
        }
    }
}
