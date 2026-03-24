using System.ComponentModel;
using System.Globalization;

namespace ConverterSplitter.Services;

public class Loc : INotifyPropertyChanged
{
    public static Loc I { get; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _lang;

    public string CurrentLanguage => _lang;
    public bool IsPolish => _lang == "pl";
    public bool IsEnglish => _lang == "en";

    public Loc()
    {
        var sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _lang = sysLang == "pl" ? "pl" : "en";
    }

    public string this[string key] =>
        _lang == "pl"
            ? Pl.GetValueOrDefault(key, En.GetValueOrDefault(key, key))
            : En.GetValueOrDefault(key, key);

    public void SetLanguage(string lang)
    {
        if (_lang == lang) return;
        _lang = lang;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPolish)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnglish)));
    }

    public void ToggleLanguage() => SetLanguage(_lang == "pl" ? "en" : "pl");

    // ─── English ───────────────────────────────────────────
    private static readonly Dictionary<string, string> En = new()
    {
        // Nav sections
        ["nav_media"] = "MEDIA",
        ["nav_documents"] = "DOCUMENTS",
        ["nav_images"] = "IMAGES",

        // Nav items
        ["nav_video_mp3"] = "Video \u2192 MP3",
        ["nav_audio_cutter"] = "Audio Cutter",
        ["nav_audio_converter"] = "Audio Converter",
        ["nav_pdf_merge"] = "PDF Merge",
        ["nav_pdf_split"] = "PDF Split",
        ["nav_image_converter"] = "Image Converter",

        // Common
        ["browse_files"] = "Browse Files",
        ["browse_file"] = "Browse File",
        ["browse_pdfs"] = "Browse PDFs",
        ["browse_images"] = "Browse Images",
        ["browse_pdf"] = "Browse PDF",
        ["clear_all"] = "Clear All",
        ["convert_all"] = "Convert All",
        ["open_file"] = "Open File",
        ["open_folder"] = "Open Folder",
        ["settings"] = "Settings",
        ["output_folder"] = "Output Folder (optional)",
        ["output_folder_hint"] = "Same as source",
        ["load_another"] = "Load Another",
        ["ready"] = "Ready",
        ["done"] = "Done",
        ["converting"] = "Converting...",
        ["pages"] = "pages",
        ["page"] = "Page",
        ["of"] = "of",

        // Drag & drop
        ["drag_video"] = "Drag & drop video files here",
        ["drag_audio"] = "Drag & drop audio files here",
        ["drag_audio_single"] = "Drag & drop an audio file here",
        ["drag_pdf"] = "Drag & drop PDF files here",
        ["drag_pdf_single"] = "Drag & drop a PDF file here",
        ["drag_images"] = "Drag & drop images here",

        // Video
        ["video_title"] = "Video \u2192 MP3 Converter",
        ["video_desc"] = "Convert video files to MP3 audio. Supports MP4, AVI, MKV, MOV and more.",
        ["video_bitrate"] = "Bitrate (kbps)",
        ["video_ffmpeg_warn"] = "FFmpeg not found! Install FFmpeg and add it to PATH.",
        ["video_status_ready"] = "Drag & drop video files or click Browse",
        ["video_status_done"] = "Conversion complete! {0}/{1} files converted.",

        // Audio Cutter
        ["cutter_title"] = "Audio Cutter",
        ["cutter_desc"] = "Load an audio file, select a region, and export the cut segment.",
        ["cutter_waveform"] = "Waveform",
        ["cutter_start"] = "Selection Start (seconds)",
        ["cutter_end"] = "Selection End (seconds)",
        ["cutter_selection"] = "Selection",
        ["cutter_play_sel"] = "Play Selection",
        ["cutter_cut_save"] = "Cut & Save",
        ["cutter_status_load"] = "Load an audio file to begin",
        ["cutter_status_loaded"] = "Loaded: {0} ({1})",
        ["cutter_status_cutting"] = "Cutting audio...",
        ["cutter_status_saved"] = "Saved: {0}",
        ["cutter_duration"] = "Duration:",

        // Audio Converter
        ["audio_title"] = "Audio Format Converter",
        ["audio_desc"] = "Convert audio files between formats: MP3, WAV, FLAC, OGG, AAC and more.",
        ["audio_format"] = "Output Format",
        ["audio_bitrate"] = "Bitrate (kbps)",
        ["audio_sample_rate"] = "Sample Rate (Hz)",
        ["audio_status_ready"] = "Drag & drop audio files or click Browse",
        ["audio_status_done"] = "Conversion complete! {0}/{1} files.",
        ["audio_includes"] = "Includes iPhone & Android dictaphone formats",

        // PDF Merge
        ["merge_title"] = "PDF Merge",
        ["merge_desc"] = "Combine multiple PDF files into one. Drag to reorder.",
        ["merge_btn"] = "Merge PDFs",
        ["merge_status_ready"] = "Drag & drop PDF files or click Browse",
        ["merge_status_done"] = "Merged {0} PDFs into {1}",
        ["merge_need_two"] = "Need at least 2 PDF files to merge",
        ["merge_pages_total"] = "{0} PDF(s) ready to merge ({1} pages total)",
        ["merge_preview"] = "Preview",

        // PDF Split
        ["split_title"] = "PDF Split",
        ["split_desc"] = "Split a PDF into individual pages or custom ranges.",
        ["split_mode_pages"] = "Split into individual pages",
        ["split_mode_pages_desc"] = "Each page becomes a separate PDF file",
        ["split_mode_ranges"] = "Split by custom ranges",
        ["split_mode_ranges_desc"] = "e.g. 1-3, 4-6, 7-10",
        ["split_btn"] = "Split PDF",
        ["split_extract"] = "Extract Pages",
        ["split_status_load"] = "Load a PDF file to split",
        ["split_status_loaded"] = "Loaded: {0} ({1} pages)",
        ["split_status_split_pages"] = "Split into {0} individual pages",
        ["split_status_split_parts"] = "Split into {0} parts",
        ["split_ranges_hint"] = "Page ranges (e.g. 1-3, 4-6, 7-10)",
        ["split_page_overview"] = "Page Overview",
        ["split_mode"] = "Split Mode",

        // Image
        ["img_title"] = "Image Converter & Resizer",
        ["img_desc"] = "Convert images between formats and optionally resize them.",
        ["img_format"] = "Output Format",
        ["img_quality"] = "Quality (JPEG/WebP)",
        ["img_resize"] = "Resize images",
        ["img_width"] = "Width (px)",
        ["img_height"] = "Height (px)",
        ["img_keep_ratio"] = "Keep aspect ratio",
        ["img_settings"] = "Conversion Settings",
        ["img_status_ready"] = "Drag & drop images or click Browse",
        ["img_status_done"] = "Converted {0}/{1} images.",
        ["img_formats_hint"] = "PNG, JPG, BMP, GIF, WebP, TIFF",

        // Update
        ["update_available"] = "Update available",
        ["update_check"] = "Check for updates",
        ["update_title"] = "Update Available!",
        ["update_current"] = "Current",
        ["update_latest"] = "Latest",
        ["update_released"] = "Released:",
        ["update_download_install"] = "Download & Install",
        ["update_view_github"] = "View on GitHub",
        ["update_later"] = "Later",
        ["update_downloading"] = "Downloading update...",
        ["update_cancelled"] = "Update cancelled.",
        ["update_failed"] = "Update failed:",

        // FFmpeg
        ["ffmpeg_title"] = "FFmpeg Required",
        ["ffmpeg_desc"] = "FFmpeg is needed for video and audio processing.\nClick Download to install it automatically.",
        ["ffmpeg_download"] = "Download FFmpeg (~80 MB)",
        ["ffmpeg_skip"] = "Skip",
        ["ffmpeg_installed"] = "FFmpeg installed successfully!",
        ["ffmpeg_cancelled"] = "Download cancelled.",
        ["ffmpeg_failed"] = "Download failed:",
        ["ffmpeg_retry"] = "You can retry or install FFmpeg manually.",

        // Language
        ["lang_switch"] = "Switch language",
    };

    // ─── Polish ────────────────────────────────────────────
    private static readonly Dictionary<string, string> Pl = new()
    {
        ["nav_media"] = "MEDIA",
        ["nav_documents"] = "DOKUMENTY",
        ["nav_images"] = "OBRAZY",

        ["nav_video_mp3"] = "Video \u2192 MP3",
        ["nav_audio_cutter"] = "Przycinanie Audio",
        ["nav_audio_converter"] = "Konwerter Audio",
        ["nav_pdf_merge"] = "Scalanie PDF",
        ["nav_pdf_split"] = "Dzielenie PDF",
        ["nav_image_converter"] = "Konwerter Obraz\u00f3w",

        ["browse_files"] = "Przegl\u0105daj pliki",
        ["browse_file"] = "Przegl\u0105daj plik",
        ["browse_pdfs"] = "Przegl\u0105daj PDF",
        ["browse_images"] = "Przegl\u0105daj obrazy",
        ["browse_pdf"] = "Przegl\u0105daj PDF",
        ["clear_all"] = "Wyczy\u015b\u0107",
        ["convert_all"] = "Konwertuj",
        ["open_file"] = "Otw\u00f3rz plik",
        ["open_folder"] = "Otw\u00f3rz folder",
        ["settings"] = "Ustawienia",
        ["output_folder"] = "Folder wyj\u015bciowy (opcjonalnie)",
        ["output_folder_hint"] = "Tak jak \u017ar\u00f3d\u0142o",
        ["load_another"] = "Za\u0142aduj inny",
        ["ready"] = "Gotowe",
        ["done"] = "Gotowe",
        ["converting"] = "Konwertowanie...",
        ["pages"] = "stron",
        ["page"] = "Strona",
        ["of"] = "z",

        ["drag_video"] = "Przeci\u0105gnij i upu\u015b\u0107 pliki wideo",
        ["drag_audio"] = "Przeci\u0105gnij i upu\u015b\u0107 pliki audio",
        ["drag_audio_single"] = "Przeci\u0105gnij i upu\u015b\u0107 plik audio",
        ["drag_pdf"] = "Przeci\u0105gnij i upu\u015b\u0107 pliki PDF",
        ["drag_pdf_single"] = "Przeci\u0105gnij i upu\u015b\u0107 plik PDF",
        ["drag_images"] = "Przeci\u0105gnij i upu\u015b\u0107 obrazy",

        ["video_title"] = "Konwerter Video \u2192 MP3",
        ["video_desc"] = "Konwertuj pliki wideo do MP3. Obs\u0142uguje MP4, AVI, MKV, MOV i inne.",
        ["video_bitrate"] = "Bitrate (kbps)",
        ["video_ffmpeg_warn"] = "FFmpeg nie znaleziony! Zainstaluj FFmpeg i dodaj do PATH.",
        ["video_status_ready"] = "Przeci\u0105gnij pliki wideo lub kliknij Przegl\u0105daj",
        ["video_status_done"] = "Konwersja zako\u0144czona! {0}/{1} plik\u00f3w.",

        ["cutter_title"] = "Przycinanie Audio",
        ["cutter_desc"] = "Za\u0142aduj plik audio, wybierz fragment i wyeksportuj.",
        ["cutter_waveform"] = "Przebieg",
        ["cutter_start"] = "Pocz\u0105tek zaznaczenia (sek.)",
        ["cutter_end"] = "Koniec zaznaczenia (sek.)",
        ["cutter_selection"] = "Zaznaczenie",
        ["cutter_play_sel"] = "Odtw\u00f3rz zaznaczenie",
        ["cutter_cut_save"] = "Wytnij i zapisz",
        ["cutter_status_load"] = "Za\u0142aduj plik audio aby rozpocz\u0105\u0107",
        ["cutter_status_loaded"] = "Za\u0142adowano: {0} ({1})",
        ["cutter_status_cutting"] = "Wycinanie audio...",
        ["cutter_status_saved"] = "Zapisano: {0}",
        ["cutter_duration"] = "Czas trwania:",

        ["audio_title"] = "Konwerter Format\u00f3w Audio",
        ["audio_desc"] = "Konwertuj pliki audio: MP3, WAV, FLAC, OGG, AAC i inne.",
        ["audio_format"] = "Format wyj\u015bciowy",
        ["audio_bitrate"] = "Bitrate (kbps)",
        ["audio_sample_rate"] = "Cz\u0119stotliwo\u015b\u0107 (Hz)",
        ["audio_status_ready"] = "Przeci\u0105gnij pliki audio lub kliknij Przegl\u0105daj",
        ["audio_status_done"] = "Konwersja zako\u0144czona! {0}/{1} plik\u00f3w.",
        ["audio_includes"] = "Obs\u0142uguje formaty z dyktafonu iPhone i Android",

        ["merge_title"] = "Scalanie PDF",
        ["merge_desc"] = "Po\u0142\u0105cz wiele plik\u00f3w PDF w jeden. Zmie\u0144 kolejno\u015b\u0107 przyciskami.",
        ["merge_btn"] = "Po\u0142\u0105cz PDF",
        ["merge_status_ready"] = "Przeci\u0105gnij pliki PDF lub kliknij Przegl\u0105daj",
        ["merge_status_done"] = "Po\u0142\u0105czono {0} PDF w {1}",
        ["merge_need_two"] = "Potrzeba min. 2 plik\u00f3w PDF",
        ["merge_pages_total"] = "{0} PDF gotowych ({1} stron \u0142\u0105cznie)",
        ["merge_preview"] = "Podgl\u0105d",

        ["split_title"] = "Dzielenie PDF",
        ["split_desc"] = "Podziel PDF na pojedyncze strony lub zakresy.",
        ["split_mode_pages"] = "Podziel na pojedyncze strony",
        ["split_mode_pages_desc"] = "Ka\u017cda strona jako osobny plik PDF",
        ["split_mode_ranges"] = "Podziel wg zakres\u00f3w",
        ["split_mode_ranges_desc"] = "np. 1-3, 4-6, 7-10",
        ["split_btn"] = "Podziel PDF",
        ["split_extract"] = "Wyodr\u0119bnij strony",
        ["split_status_load"] = "Za\u0142aduj plik PDF do podzia\u0142u",
        ["split_status_loaded"] = "Za\u0142adowano: {0} ({1} stron)",
        ["split_status_split_pages"] = "Podzielono na {0} stron",
        ["split_status_split_parts"] = "Podzielono na {0} cz\u0119\u015bci",
        ["split_ranges_hint"] = "Zakresy stron (np. 1-3, 4-6, 7-10)",
        ["split_page_overview"] = "Podgl\u0105d stron",
        ["split_mode"] = "Tryb podzia\u0142u",

        ["img_title"] = "Konwerter i Skalowanie Obraz\u00f3w",
        ["img_desc"] = "Konwertuj obrazy mi\u0119dzy formatami i opcjonalnie zmieniaj rozmiar.",
        ["img_format"] = "Format wyj\u015bciowy",
        ["img_quality"] = "Jako\u015b\u0107 (JPEG/WebP)",
        ["img_resize"] = "Zmie\u0144 rozmiar",
        ["img_width"] = "Szeroko\u015b\u0107 (px)",
        ["img_height"] = "Wysoko\u015b\u0107 (px)",
        ["img_keep_ratio"] = "Zachowaj proporcje",
        ["img_settings"] = "Ustawienia konwersji",
        ["img_status_ready"] = "Przeci\u0105gnij obrazy lub kliknij Przegl\u0105daj",
        ["img_status_done"] = "Skonwertowano {0}/{1} obraz\u00f3w.",
        ["img_formats_hint"] = "PNG, JPG, BMP, GIF, WebP, TIFF",

        ["update_available"] = "Dost\u0119pna aktualizacja",
        ["update_check"] = "Sprawd\u017a aktualizacje",
        ["update_title"] = "Dost\u0119pna aktualizacja!",
        ["update_current"] = "Aktualna",
        ["update_latest"] = "Najnowsza",
        ["update_released"] = "Wydano:",
        ["update_download_install"] = "Pobierz i zainstaluj",
        ["update_view_github"] = "Zobacz na GitHub",
        ["update_later"] = "P\u00f3\u017aniej",
        ["update_downloading"] = "Pobieranie aktualizacji...",
        ["update_cancelled"] = "Anulowano.",
        ["update_failed"] = "B\u0142\u0105d pobierania:",

        ["ffmpeg_title"] = "Wymagany FFmpeg",
        ["ffmpeg_desc"] = "FFmpeg jest potrzebny do przetwarzania wideo i audio.\nKliknij Pobierz, aby zainstalowa\u0107 automatycznie.",
        ["ffmpeg_download"] = "Pobierz FFmpeg (~80 MB)",
        ["ffmpeg_skip"] = "Pomi\u0144",
        ["ffmpeg_installed"] = "FFmpeg zainstalowany pomy\u015blnie!",
        ["ffmpeg_cancelled"] = "Pobieranie anulowane.",
        ["ffmpeg_failed"] = "B\u0142\u0105d pobierania:",
        ["ffmpeg_retry"] = "Spr\u00f3buj ponownie lub zainstaluj FFmpeg r\u0119cznie.",

        ["lang_switch"] = "Zmie\u0144 j\u0119zyk",
    };
}
