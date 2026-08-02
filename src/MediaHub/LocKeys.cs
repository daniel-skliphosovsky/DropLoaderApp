namespace MediaHub;

/// <summary>
/// Localization keys used from C# via Loc.Get. Centralizing them removes the
/// magic strings scattered across the view models, downloaders and services
/// and makes renames refactor-safe.
/// </summary>
public static class LocKeys
{
    // App header
    public const string AppSubtitle = "App.Subtitle";
    public const string GithubHint = "Github.Hint";
    public const string ThemeToggleHint = "Theme.ToggleHint";
    public const string LangHint = "Lang.Hint";
    public const string LangCodeEn = "Lang.CodeEn";
    public const string LangCodeRu = "Lang.CodeRu";

    // URL input
    public const string UrlLabel = "Url.Label";
    public const string UrlPlaceholder = "Url.Placeholder";
    public const string UrlHint = "Url.Hint";

    // Actions
    public const string DownloadButton = "Download.Button";
    public const string DownloadHint = "Download.Hint";
    public const string InfoButton = "Info.Button";
    public const string InfoHint = "Info.Hint";

    // Save folder
    public const string SaveToLabel = "SaveTo.Label";
    public const string SaveToPathHint = "SaveTo.PathHint";
    public const string SaveToBrowse = "SaveTo.Browse";
    public const string SaveToBrowseHint = "SaveTo.BrowseHint";
    public const string SaveToPlaceholder = "SaveTo.Placeholder";

    // Progress popup
    public const string PopupStop = "Popup.Stop";
    public const string PopupStopHint = "Popup.StopHint";
    public const string PopupLoading = "Popup.Loading";
    public const string PopupClose = "Popup.Close";
    public const string PopupCloseHint = "Popup.CloseHint";

    // Status lines
    public const string StatusAutoDetect = "Status.AutoDetect";
    public const string StatusUnknown = "Status.Unknown";
    public const string StatusStarting = "Status.Starting";
    public const string StatusDownloading = "Status.Downloading";
    public const string StatusTrackOf = "Status.TrackOf";
    public const string StatusPreviewError = "Status.PreviewError";
    public const string StatusOf = "Status.Of";
    public const string StatusPerSecond = "Status.PerSecond";
    public const string StatusDownloadName = "Status.DownloadName";

    // Dialogs
    public const string DialogError = "Dialog.Error";
    public const string DialogOk = "Dialog.Ok";
    public const string DialogYes = "Dialog.Yes";
    public const string DialogNo = "Dialog.No";
    public const string DialogSuccess = "Dialog.Success";
    public const string DialogNoFolderTitle = "Dialog.NoFolderTitle";
    public const string DialogNoFolderMessage = "Dialog.NoFolderMessage";
    public const string DialogUnsupportedTitle = "Dialog.UnsupportedTitle";
    public const string DialogUnsupportedMessage = "Dialog.UnsupportedMessage";
    public const string DialogEmptyPlaylist = "Dialog.EmptyPlaylist";
    public const string DialogSavedTo = "Dialog.SavedTo";
    public const string DialogSomeFailed = "Dialog.SomeFailed";
    public const string DialogGenericError = "Dialog.GenericError";
    public const string DialogUnexpectedTitle = "Dialog.UnexpectedTitle";
    public const string DialogUnexpectedMessage = "Dialog.UnexpectedMessage";
    public const string DialogFatal = "Dialog.Fatal";
    public const string DialogPickFolderError = "Dialog.PickFolderError";
    public const string DialogPopupError = "Dialog.PopupError";

    // Downloader errors
    public const string ErrNoStream = "Err.NoStream";
    public const string ErrTrackNotFound = "Err.TrackNotFound";
    public const string ErrNoContent = "Err.NoContent";
    public const string ErrCancelled = "Err.Cancelled";
    public const string ErrSoundCloudNotDownloadable = "Err.SoundCloudNotDownloadable";
    public const string ErrPlatformPrefix = "Err.PlatformPrefix";
    public const string ErrScrapeNoStream = "Err.ScrapeNoStream";
    public const string ErrVkNoStream = "Err.VkNoStream";
    public const string ErrVideoNotFound = "Err.VideoNotFound";
    public const string ErrVideoInfo = "Err.VideoInfo";
    public const string ErrOkNoStream = "Err.OkNoStream";
    public const string ErrDailymotionNoStream = "Err.DailymotionNoStream";
    public const string ErrVimeoNoStream = "Err.VimeoNoStream";
    public const string ErrVimeoRateLimited = "Err.VimeoRateLimited";

    // Platform names
    public const string PlatformOk = "Platform.Ok";
    public const string PlatformDailymotion = "Platform.Dailymotion";
    public const string PlatformVimeo = "Platform.Vimeo";

    // Quality labels
    public const string QualityAudio = "Quality.Audio";
    public const string QualityVideo = "Quality.Video";
    public const string QualityImages = "Quality.Images";

    // Metadata details
    public const string DetailsTitle = "Details.Title";
    public const string DetailsAuthor = "Details.Author";
    public const string DetailsDuration = "Details.Duration";
    public const string DetailsUploaded = "Details.Uploaded";
    public const string DetailsViews = "Details.Views";
    public const string DetailsLikes = "Details.Likes";
    public const string DetailsDescription = "Details.Description";
    public const string DetailsKeywords = "Details.Keywords";
    public const string DetailsGenre = "Details.Genre";
    public const string DetailsPlays = "Details.Plays";
    public const string DetailsComments = "Details.Comments";
    public const string DetailsDownloads = "Details.Downloads";
    public const string DetailsDownloadable = "Details.Downloadable";
    public const string DetailsPosted = "Details.Posted";
    public const string DetailsLink = "Details.Link";
    public const string DetailsAuthorId = "Details.AuthorId";
    public const string DetailsRegion = "Details.Region";
    public const string DetailsVerified = "Details.Verified";
    public const string DetailsYes = "Details.Yes";
    public const string DetailsShares = "Details.Shares";
    public const string DetailsSound = "Details.Sound";

    // Info popup
    public const string InfoNoInfo = "Info.NoInfo";
    public const string InfoLoadError = "Info.LoadError";

    // Progress phases
    public const string ProgressFetching = "Progress.Fetching";
    public const string ProgressFetchingMetadata = "Progress.FetchingMetadata";
    public const string ProgressDownloadingVideo = "Progress.DownloadingVideo";
    public const string ProgressDownloadingImages = "Progress.DownloadingImages";
    public const string ProgressDownloadingFile = "Progress.DownloadingFile";
    public const string ProgressDownloading = "Progress.Downloading";
}
