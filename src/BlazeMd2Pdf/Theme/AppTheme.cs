using MudBlazor;

namespace BlazeMd2Pdf.Theme;

public static class AppTheme
{
    public static MudTheme Md2Pdf => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#512BD4",          
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#0077FF",        
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#FF5252",       
            TertiaryContrastText = "#FFFFFF",

            AppbarBackground = "#512BD4",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#F8F9FA",
            DrawerText = "#212529",
            DrawerIcon = "#495057",

            Background = "#F3F4F6",      
            Surface = "#FFFFFF",         

            TextPrimary = "#1F2937",
            TextSecondary = "#4B5563",
            TextDisabled = "#9CA3AF",

            ActionDefault = "#4B5563",
            ActionDisabled = "#D1D5DB",
            ActionDisabledBackground = "#E5E7EB",

            Divider = "#E5E7EB",
            LinesDefault = "#D1D5DB",
            LinesInputs = "#9CA3AF",

            Success = "#10B981",
            Warning = "#F59E0B",
            Error = "#EF4444",
            Info = "#3B82F6",

            Dark = "#111827"
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#7C4DFF",          
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#3B82F6",        
            SecondaryContrastText = "#121212",
            Tertiary = "#FF8A80",         
            TertiaryContrastText = "#121212",

            AppbarBackground = "#1E1E2F",
            AppbarText = "#E5E7EB",
            DrawerBackground = "#181824",
            DrawerText = "#E5E7EB",
            DrawerIcon = "#9CA3AF",

            Background = "#121212",       
            Surface = "#1E1E2F",          

            TextPrimary = "#F3F4F6",
            TextSecondary = "#9CA3AF",
            TextDisabled = "#6B7280",

            ActionDefault = "#9CA3AF",
            ActionDisabled = "#4B5563",
            ActionDisabledBackground = "#1F2937",

            Divider = "#2D2D3D",
            LinesDefault = "#2D2D3D",
            LinesInputs = "#4B5563",

            Success = "#34D399",
            Warning = "#FBBF24",
            Error = "#F87171",
            Info = "#60A5FA",

            Dark = "#F3F4F6"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.43",
                LetterSpacing = ".01071em"
            },
            H1 = new H1Typography { FontSize = "3.5rem", FontWeight = "600", LineHeight = "1.167", LetterSpacing = "-.01562em" },
            H2 = new H2Typography { FontSize = "3rem", FontWeight = "600", LineHeight = "1.2", LetterSpacing = "-.00833em" },
            H3 = new H3Typography { FontSize = "2.25rem", FontWeight = "600", LineHeight = "1.167", LetterSpacing = "0em" },
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "500", LineHeight = "1.235", LetterSpacing = ".00735em" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "500", LineHeight = "1.334", LetterSpacing = "0em" },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.6", LetterSpacing = ".0075em" },
            Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.75", LetterSpacing = ".02857em" }
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px"
        },

        Shadows = new Shadow()
    };
}