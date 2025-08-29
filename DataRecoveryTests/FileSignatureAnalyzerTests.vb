Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports DataRecoveryCore.FileSignatures
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Logging.Abstractions

''' <summary>
''' Test suite for FileSignatureAnalyzer - validates file type detection and signature analysis
''' Ensures accurate identification of various file formats through binary signatures
''' </summary>
<TestClass>
Public Class FileSignatureAnalyzerTests

    Private analyzer As FileSignatureAnalyzer
    Private logger As ILogger

    <TestInitialize>
    Public Sub Setup()
        logger = NullLogger.Instance
        analyzer = New FileSignatureAnalyzer(logger)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_JpegSignature_ReturnsJpegInfo()
        ' Arrange - Create JPEG file signature
        Dim jpegHeader As Byte() = {&HFF, &HD8, &HFF, &HE0, &H0, &H10, &H4A, &H46, &H49, &H46}
        
        ' Act
        Dim result = analyzer.DetectFileType(jpegHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("JPEG Image", result.Description)
        Assert.AreEqual(".jpg", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("jpg"))
        Assert.IsTrue(result.KnownExtensions.Contains("jpeg"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_PngSignature_ReturnsPngInfo()
        ' Arrange - Create PNG file signature
        Dim pngHeader As Byte() = {&H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA}
        
        ' Act
        Dim result = analyzer.DetectFileType(pngHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("PNG Image", result.Description)
        Assert.AreEqual(".png", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("png"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_PdfSignature_ReturnsPdfInfo()
        ' Arrange - Create PDF file signature
        Dim pdfHeader As Byte() = {&H25, &H50, &H44, &H46, &H2D} ' %PDF-
        
        ' Act
        Dim result = analyzer.DetectFileType(pdfHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("PDF Document", result.Description)
        Assert.AreEqual(".pdf", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("pdf"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_ZipSignature_ReturnsZipInfo()
        ' Arrange - Create ZIP file signature
        Dim zipHeader As Byte() = {&H50, &H4B, &H3, &H4} ' PK..
        
        ' Act
        Dim result = analyzer.DetectFileType(zipHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("ZIP Archive", result.Description)
        Assert.AreEqual(".zip", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("zip"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_Mp3Signature_ReturnsMp3Info()
        ' Arrange - Create MP3 file signature (ID3v2)
        Dim mp3Header As Byte() = {&H49, &H44, &H33} ' ID3
        
        ' Act
        Dim result = analyzer.DetectFileType(mp3Header)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("MP3 Audio", result.Description)
        Assert.AreEqual(".mp3", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("mp3"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_UnknownSignature_ReturnsNull()
        ' Arrange - Create unknown file signature
        Dim unknownHeader As Byte() = {&H99, &H99, &H99, &H99, &H99}
        
        ' Act
        Dim result = analyzer.DetectFileType(unknownHeader)
        
        ' Assert
        Assert.IsNull(result)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_EmptyArray_ReturnsNull()
        ' Arrange
        Dim emptyHeader As Byte() = {}
        
        ' Act
        Dim result = analyzer.DetectFileType(emptyHeader)
        
        ' Assert
        Assert.IsNull(result)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_NullArray_ReturnsNull()
        ' Arrange
        Dim nullHeader As Byte() = Nothing
        
        ' Act
        Dim result = analyzer.DetectFileType(nullHeader)
        
        ' Assert
        Assert.IsNull(result)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_MicrosoftOfficeSignature_ReturnsOfficeInfo()
        ' Arrange - Create MS Office compound document signature
        Dim officeHeader As Byte() = {&HD0, &HCF, &H11, &HE0, &HA1, &HB1, &H1A, &HE1}
        
        ' Act
        Dim result = analyzer.DetectFileType(officeHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("Microsoft Office Document", result.Description)
        Assert.AreEqual(".doc", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("doc"))
        Assert.IsTrue(result.KnownExtensions.Contains("xls"))
        Assert.IsTrue(result.KnownExtensions.Contains("ppt"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_ExeSignature_ReturnsExeInfo()
        ' Arrange - Create Windows executable signature
        Dim exeHeader As Byte() = {&H4D, &H5A} ' MZ
        
        ' Act
        Dim result = analyzer.DetectFileType(exeHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("Windows Executable", result.Description)
        Assert.AreEqual(".exe", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("exe"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_GifSignature_ReturnsGifInfo()
        ' Arrange - Create GIF file signature (GIF87a)
        Dim gifHeader As Byte() = {&H47, &H49, &H46, &H38, &H37, &H61} ' GIF87a
        
        ' Act
        Dim result = analyzer.DetectFileType(gifHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("GIF Image", result.Description)
        Assert.AreEqual(".gif", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("gif"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_TiffSignature_ReturnsTiffInfo()
        ' Arrange - Create TIFF file signature (little-endian)
        Dim tiffHeader As Byte() = {&H49, &H49, &H2A, &H0} ' II*.
        
        ' Act
        Dim result = analyzer.DetectFileType(tiffHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("TIFF Image", result.Description)
        Assert.AreEqual(".tiff", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("tiff"))
        Assert.IsTrue(result.KnownExtensions.Contains("tif"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_RarSignature_ReturnsRarInfo()
        ' Arrange - Create RAR archive signature
        Dim rarHeader As Byte() = {&H52, &H61, &H72, &H21, &H1A, &H7, &H0} ' Rar!...
        
        ' Act
        Dim result = analyzer.DetectFileType(rarHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("RAR Archive", result.Description)
        Assert.AreEqual(".rar", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("rar"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_7ZipSignature_Returns7ZipInfo()
        ' Arrange - Create 7-Zip archive signature
        Dim sevenZipHeader As Byte() = {&H37, &H7A, &HBC, &HAF, &H27, &H1C} ' 7z¼¯'..
        
        ' Act
        Dim result = analyzer.DetectFileType(sevenZipHeader)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("7-Zip Archive", result.Description)
        Assert.AreEqual(".7z", result.PrimaryExtension)
        Assert.IsTrue(result.IsValid)
        Assert.IsTrue(result.KnownExtensions.Contains("7z"))
    End Sub

    <TestMethod>
    Public Sub DetectFileType_PartialSignature_ReturnsCorrectMatch()
        ' Arrange - Test with partial signature data (common in fragmented files)
        Dim partialJpegHeader As Byte() = {&HFF, &HD8, &HFF} ' Only first 3 bytes
        
        ' Act
        Dim result = analyzer.DetectFileType(partialJpegHeader)
        
        ' Assert
        Assert.IsNotNull(result) ' Should still detect JPEG with partial signature
        Assert.AreEqual("JPEG Image", result.Description)
        Assert.IsTrue(result.IsValid)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_LongDataArray_HandlesCorrectly()
        ' Arrange - Create array longer than typical signature
        Dim longData(1024) As Byte
        longData(0) = &H89 ' PNG signature start
        longData(1) = &H50
        longData(2) = &H4E
        longData(3) = &H47
        longData(4) = &HD
        longData(5) = &HA
        longData(6) = &H1A
        longData(7) = &HA
        
        ' Fill rest with random data
        For i As Integer = 8 To longData.Length - 1
            longData(i) = CByte(i Mod 256)
        Next
        
        ' Act
        Dim result = analyzer.DetectFileType(longData)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("PNG Image", result.Description)
        Assert.IsTrue(result.IsValid)
    End Sub

    <TestMethod>
    Public Sub DetectFileType_MultipleMatches_ReturnsFirstMatch()
        ' Arrange - Create data that might match multiple signatures
        ' This tests the priority/ordering of signature detection
        Dim ambiguousData As Byte() = {&H50, &H4B, &H3, &H4} ' ZIP signature
        
        ' Act
        Dim result = analyzer.DetectFileType(ambiguousData)
        
        ' Assert
        Assert.IsNotNull(result)
        Assert.AreEqual("ZIP Archive", result.Description) ' Should prioritize ZIP over other PK formats
        Assert.IsTrue(result.IsValid)
    End Sub

    <TestCleanup>
    Public Sub Cleanup()
        analyzer = Nothing
        logger = Nothing
    End Sub

End Class