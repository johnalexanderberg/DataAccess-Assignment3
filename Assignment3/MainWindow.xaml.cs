using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GeographyTools;
using Windows.Devices.Geolocation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Assignment3
{

    public class Ticket
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public Screening Screening { get; set; }
        public DateTime TimePurchased { get; set; }
    }
    public class Cinema
    {
        [Key]
        public int ID { get; set; }
        [MaxLength(255), Required]
        public string Name { get; set; }
        [MaxLength(255), Required]
        public string City { get; set; }
        [Required, Column(TypeName = "float")]
        public float Latitude { get; set; }
        [Required, Column(TypeName = "float")]
        public float Longitude { get; set; }
        public List<Screening> Screenings { get; set; } 
    }
    public class Screening
    {
        [Key]
        public int ID { get; set; }
        [Column(TypeName = "time(0)")]
        public TimeSpan Time { get; set; }
        [Required]
        public Movie Movie { get; set; }
        [Required]
        public Cinema Cinema { get; set; }
        public List<Ticket> Tickets { get; set; }
    }
    public class Movie
    {
        [Key]
        public int ID { get; set; }
        [MaxLength(255), Required]
        public string Title { get; set; }
        [Required]
        public short Runtime { get; set; }
        [Column(TypeName = "date")]
        public DateTime ReleaseDate { get; set; }
        [MaxLength(255), Required]
        public string PosterPath { get; set; }
        public List<Screening> Screenings { get; set; }
    }
    public class AppDbContext : DbContext
    {
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Screening> Screenings { get; set; }
        public DbSet<Movie> Movies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS01;Initial Catalog=DataAccessGUIAssignment;Integrated Security=True");
        }
    }

    public partial class MainWindow : Window
    {
        private Thickness spacing = new Thickness(5);
        private FontFamily mainFont = new FontFamily("Constantia");

        // Some GUI elements that we need to access in multiple methods.
        private ComboBox cityComboBox;
        private ListBox cinemaListBox;
        private StackPanel screeningPanel;
        private StackPanel ticketPanel;

        // An SQL connection that we will keep open for the entire program.
        private AppDbContext database;




        public MainWindow()
        {
            InitializeComponent();

            Start();
        }

        private async void Start()
        {

            database = new AppDbContext();

            // Window options
            Title = "Cinemania";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Black;

            // Main grid
            var grid = new Grid();
            Content = grid;
            grid.Margin = spacing;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var cinemaGui = await CreateCinemaGUI();
            var screeningGui = await CreateScreeningGUIAsync();
            var ticketGui = await CreateTicketGUIAsync();

            AddToGrid(grid, cinemaGui, 0, 0);
            AddToGrid(grid, screeningGui, 0, 1);
            AddToGrid(grid, ticketGui, 0, 2);
          
        }

        // Create the cinema part of the GUI: the left column.
        private async Task<UIElement> CreateCinemaGUI()
        {
            var grid = new Grid
            {
                MinWidth = 200
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Cinema",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            // Create the dropdown of cities.
            cityComboBox = new ComboBox
            {
                Margin = spacing
            };
            var cities = await GetCitiesAsync();
          

            foreach (string city in cities)
            {
                cityComboBox.Items.Add(city);
            }
            cityComboBox.SelectedIndex = 0;
            AddToGrid(grid, cityComboBox, 1, 0);

            // When we select a city, update the GUI with the cinemas in the currently selected city.
            cityComboBox.SelectionChanged += async(sender, e) =>
            {
                await UpdateCinemaListAsync();

            };

            // Create the dropdown of cinemas.
            cinemaListBox = new ListBox
            {
                Margin = spacing
            };
            AddToGrid(grid, cinemaListBox, 2, 0);

            await UpdateCinemaListAsync();

            // When we select a cinema, update the GUI with the screenings in the currently selected cinema.
            cinemaListBox.SelectionChanged += async(sender, e) =>
            {
                await UpdateScreeningListAsync();
            };

            return grid;
        }

        // Create the screening part of the GUI: the middle column.
        private async Task<UIElement> CreateScreeningGUIAsync()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Screening",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            screeningPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = screeningPanel;

            await UpdateScreeningListAsync();

            return grid;
        }

        // Create the ticket part of the GUI: the right column.
        private async Task<UIElement> CreateTicketGUIAsync()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "My Tickets",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            ticketPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = ticketPanel;

            // Update the GUI with the initial list of tickets.
            await UpdateTicketListAsync();

            return grid;
        }

        // Get a list of all cities that have cinemas in them.
        private async Task<List<string>> GetCitiesAsync()
        {

            CultureInfo culture = new CultureInfo("sv-SE");

            var cities = await database.Cinemas.Select(c => c.City).Distinct().ToListAsync();

            //We had to order the list client-side to get å ä ö to work.
            cities = cities.OrderBy(c => c, StringComparer.Create(culture, false)).ToList();

            return cities;
        }

        // Get a list of all cinemas in the currently selected city.
        private Task<List<string>> GetCinemasInSelectedCity()
        {
            string currentCity = (string)cityComboBox.SelectedItem;
            var cinemas = database.Cinemas.Where(c => c.City == currentCity).Select(c => c.Name).OrderBy(c => c).ToListAsync();         
            return cinemas;
        }

        // Update the GUI with the cinemas in the currently selected city.
        private async Task UpdateCinemaListAsync()
        {
            cinemaListBox.Items.Clear();

            var cinemasTask = GetCinemasInSelectedCity();
            var cinemas = await cinemasTask;

            foreach (string cinema in cinemas)
            {
                cinemaListBox.Items.Add(cinema);
            }
        }

        // Update the GUI with the screenings in the currently selected cinema.
        private async Task UpdateScreeningListAsync()
        {
            screeningPanel.Children.Clear();
            if (cinemaListBox.SelectedIndex == -1)
            {
                return;
            }

            Cinema cinema = database.Cinemas.First(n => n.Name == (string)cinemaListBox.SelectedItem);

            var screenings = await database.Screenings.Where(s => s.Cinema == cinema).Include(s => s.Movie).OrderBy(s => s.Time).ToListAsync();
            


            // For each screening:
            foreach (Screening screening in screenings)
            {
                // Create the button that will show all the info about the screening and let us buy a ticket for it.
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = spacing,
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                screeningPanel.Children.Add(button);
                int screeningID = screening.ID;

                // When we click a screening, buy a ticket for it and update the GUI with the latest list of tickets.
                button.Click += async(sender, e) =>
                {
                    await BuyTicketAsync(screeningID);
                };

                // The rest of this method is just creating the GUI element for the screening.
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                button.Content = grid;

                var image = CreateImage(@"Posters\" + screening.Movie.PosterPath);
                image.Width = 50;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = screening.Movie.Title };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 3);

                var time = screening.Time;
                var timeHeading = new TextBlock
                {
                    Text = TimeSpanToString(time),
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow
                };
                AddToGrid(grid, timeHeading, 0, 1);

                var titleHeading = new TextBlock
                {
                    Text = Convert.ToString(screening.Movie.Title),
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 1, 1);

                var releaseDate = Convert.ToDateTime(screening.Movie.ReleaseDate);
                int runtimeMinutes = Convert.ToInt32(screening.Movie.Runtime);
                var runtime = TimeSpan.FromMinutes(runtimeMinutes);
                string runtimeString = runtime.Hours + "h " + runtime.Minutes + "m";
                var details = new TextBlock
                {
                    Text = "📆 " + releaseDate.Year + "     ⏳ " + runtimeString,
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    Foreground = Brushes.Silver
                };
                AddToGrid(grid, details, 2, 1);
            }
        }

        // Buy a ticket for the specified screening and update the GUI with the latest list of tickets.
        private async Task BuyTicketAsync(int screeningID)
        {
            // First check if we already have a ticket for this screening.

            Screening screening = await database.Screenings.FirstAsync(s => s.ID == screeningID);
            bool hasTicket = await database.Tickets.Where(t => t.Screening == screening).AnyAsync();

            // If we don't, add it.
            if (!hasTicket)
            {
                var ticket = new Ticket();
                ticket.Screening = screening;
                ticket.TimePurchased = DateTime.Now;

                database.Tickets.Add(ticket);
                database.SaveChanges();

                await UpdateTicketListAsync();
            }
        }

        // Update the GUI with the latest list of tickets
        private async Task UpdateTicketListAsync()
        {
            ticketPanel.Children.Clear();

            Task<List<Ticket>> ticketsTask = database.Tickets
                .Include(t => t.Screening)
                .ThenInclude(s => s.Movie)
                .Include(t => t.Screening)
                .ThenInclude(s => s.Cinema)
                .OrderBy(t => t.TimePurchased)
                .ToListAsync();

            var tickets = await ticketsTask;

            // For each ticket:
            foreach (Ticket ticket in tickets)
            {
                // Create the button that will show all the info about the ticket and let us remove it.
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = spacing,
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                ticketPanel.Children.Add(button);
                int ticketID = ticket.ID;

                // When we click a ticket, remove it and update the GUI with the latest list of tickets.
                button.Click += async(sender, e) =>
                {
                    await RemoveTicket(ticketID);
                };

                // The rest of this method is just creating the GUI element for the screening.
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                button.Content = grid;

                var image = CreateImage(@"Posters\" + ticket.Screening.Movie.PosterPath);
                image.Width = 30;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = ticket.Screening.Movie.Title };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 2);

                var titleHeading = new TextBlock
                {
                    Text = ticket.Screening.Movie.Title,
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 0, 1);

                var time = ticket.Screening.Time;
                string timeString = TimeSpanToString(time);
                var timeAndCinemaHeading = new TextBlock
                {
                    Text = timeString + " - " + ticket.Screening.Cinema.Name,
                    Margin = spacing,
                    FontFamily = new FontFamily("Corbel"),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, timeAndCinemaHeading, 1, 1);
            }
        }

        // Remove the ticket for the specified screening and update the GUI with the latest list of tickets.
        private async Task RemoveTicket(int ticketID)
        {
            Ticket ticket = database.Tickets.First(ticket => ticket.ID == ticketID);
            database.Tickets.Remove(ticket);
            database.SaveChanges();

            await UpdateTicketListAsync();
        }

        // Helper method to add a GUI element to the specified row and column in a grid.
        private void AddToGrid(Grid grid, UIElement element, int row, int column)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        // Helper method to create a high-quality image for the GUI.
        private Image CreateImage(string filePath)
        {
            ImageSource source = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
            Image image = new Image
            {
                Source = source,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        // Helper method to turn a TimeSpan object into a string, such as 2:05.
        private string TimeSpanToString(TimeSpan timeSpan)
        {
            string hourString = timeSpan.Hours.ToString().PadLeft(2, '0');
            string minuteString = timeSpan.Minutes.ToString().PadLeft(2, '0');
            string timeString = hourString + ":" + minuteString;
            return timeString;
        }
    }
}
