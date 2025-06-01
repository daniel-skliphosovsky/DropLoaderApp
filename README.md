# DropLoaderApp
An application that helps you to download media from popular platforms

# Screenshots

### MacOS

#### Downloading Page
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingPage_LightTheme.png" width="300" alt="Light Theme Page"></td>
    <td><img src="images/MacOS/DownloadingPage_DarkTheme.png" width="300" alt="Dark Theme Page"></td>
  </tr>
  <tr>
    <td align="center">Light Theme</td>
    <td align="center">Dark Theme</td>
  </tr>
</table>

#### Downloading Process
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingContext_LightTheme.png" width="300" alt="Light Theme Context"></td>
    <td><img src="images/MacOS/DownloadingContext_DarkTheme.png" width="300" alt="Dark Theme Context"></td>
  </tr>
  <tr>
    <td align="center">Light Theme</td>
    <td align="center">Dark Theme</td>
  </tr>
</table>

#### Possible downloading finishes
<table>
  <tr>
    <td><img src="images/MacOS/DownloadingCanceled.png" width="300" alt="Downloading Canceled"></td>
    <td><img src="images/MacOS/DownloadingFinished.png" width="300" alt="Downloading Finished"></td>
  </tr>
  <tr>
    <td align="center">Downloading Canceled</td>
    <td align="center">Downloading Finished</td>
  </tr>
</table>

#### Possible Errors
<table>
  <tr>
    <td><img src="images/MacOS/ErrorType_EmptyFields.png" width="300" alt="Empty Fields Error"></td>
    <td><img src="images/MacOS/ErrorType_IncorrectLink.png" width="300" alt="Incorrect Link Error"></td>
  </tr>
  <tr>
    <td align="center">Empty Fiels</td>
    <td align="center">Incorrect Links</td>
  </tr>
</table>

# Usage
1. Open App
2. Go to «Downloading» Tab
3. Insert media link 
4. Select the path where the file will be saved
5. Click the «Download» Button to start downloading

# Possible Problems

**Soundcloud**: Some tracks may be unavailable and you may get a 403 Firbidden error -> Solution: try to find another link to this track

**TikTok**: If the publication you provided a link to is private (or does not exist) then the program will download another random video (This is related to TikTok API). Therefore, sometimes after downloading you may find a completely different video / photo. 
Also, don't be alarmed if the download doesn't start. This is also related to TikTok API -> Solution: just wait a bit (usually up to 10 seconds) and the download will start

**YouTube**: Due to changes in YouTube policy, the program is not always able to get the audio track of the video


# Technologies
.NET MAUI

##### Frontend: XAML
##### Backend: C#

