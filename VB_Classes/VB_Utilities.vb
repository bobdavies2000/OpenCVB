Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO.Pipes

Module vbUtilities
    <DllImport("gdi32.dll")>
    Public Function BitBlt(ByVal hdc As IntPtr, ByVal nXDest As Integer, ByVal nYDest As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer,
                           ByVal hdcSrc As IntPtr, ByVal nXSrc As Integer, ByVal nYSrc As Integer, ByVal dwRop As CopyPixelOperation) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Public Function FindWindow(ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Function SetForegroundWindow(ByVal hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    Public Declare Auto Function MoveWindow Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal X As Int32, ByVal Y As Int32, ByVal nWidth As Int32,
                                                              ByVal nHeight As Int32, ByVal bRepaint As Boolean) As Boolean

    Public Declare Function GetWindowRect Lib "user32" (ByVal HWND As Integer, ByRef lpRect As RECT) As Integer
    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure
    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer,
                                  ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInteger) As Boolean
    End Function

    Public gOptions As New OptionsGlobal
    Public redOptions As New OptionsRedCloud
    Public task As VBtask

    Public pipeCount As Integer
    Public openGL_hwnd As IntPtr
    Public openGLPipe As NamedPipeServerStream

    Public allOptions As OptionsContainer
    Public Const RESULT_DST0 = 0 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST1 = 1 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST2 = 2 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST3 = 3 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const screenDWidth As Integer = 18
    Public Const screenDHeight As Integer = 20
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public recordedData As Replay_Play

    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()
    Public pythonPipeIndex As Integer ' increment this for each algorithm to avoid any conflicts with other Python apps.
    Public Sub updateSettings()
        task.fpsRate = If(task.frameCount < 30, 30, task.fpsRate)
        If task.myStopWatch Is Nothing Then task.myStopWatch = Stopwatch.StartNew()

        ' update the time measures
        task.msWatch = task.myStopWatch.ElapsedMilliseconds
        quarterBeat()
        Dim frameDuration = 1000 / task.fpsRate
        task.almostHeartBeat = If(task.msWatch - task.msLast + frameDuration * 1.5 > 1000, True, False)

        If (task.msWatch - task.msLast) > 1000 Then
            task.msLast = task.msWatch
            task.toggleOn = Not task.toggleOn
            task.toggleFrame = task.frameCount - 1
        End If

        If task.paused Then
            task.midHeartBeat = False
            task.almostHeartBeat = False
        End If

        task.histogramBins = gOptions.HistBinSlider.Value
        task.lineWidth = gOptions.LineWidth.Value
        task.dotSize = gOptions.dotSizeSlider.Value
        task.AddWeighted = gOptions.AddWeightedSlider.Value / 100

        task.maxZmeters = gOptions.MaxDepth.Value
        task.SyncOutput = gOptions.SyncOutput.Checked
    End Sub
    Public Function GetWindowImage(ByVal WindowHandle As IntPtr, ByVal rect As cv.Rect) As Bitmap
        Dim b As New Bitmap(rect.Width, rect.Height, Imaging.PixelFormat.Format24bppRgb)

        Using img As Graphics = Graphics.FromImage(b)
            Dim ImageHDC As IntPtr = img.GetHdc
            Try
                Using window As Graphics = Graphics.FromHwnd(WindowHandle)
                    Dim WindowHDC As IntPtr = window.GetHdc
                    BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, CopyPixelOperation.SourceCopy)
                    window.ReleaseHdc()
                End Using
                img.ReleaseHdc()
            Catch ex As Exception
                ' ignoring the error - they probably closed the OpenGL window.
            End Try
        End Using

        Return b
    End Function
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Sub Swap(Of T)(ByRef a As T, ByRef b As T)
        Dim temp = b
        b = a
        a = temp
    End Sub
    Public Function findfrm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBoxes") Then
                        For j = 0 To frm.Box.Count - 1
                            If frm.Box(j).text = opt Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Private Function searchForms(opt As String, ByRef index As Integer)
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Buttons") Then
                        For j = 0 To frm.check.count - 1
                            If frm.check(j).text = opt Then
                                index = j
                                Return frm.check
                            End If
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A Radio button was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        Dim index As Integer
        Dim radio = searchForms(opt, index)
        If radio Is Nothing Then Return Nothing
        Return radio(index)
    End Function
    Public Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
        For Each rad In radioList
            If rad.Checked Then Return rad.Text
        Next
        Return radioList(0).Text
    End Function
    Public Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As String
        For i = 0 To radioList.Count - 1
            If radioList(i).Checked Then Return i
        Next
        Return 0
    End Function
    Public Function findSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.trackbar.Count - 1
                        If frm.sLabels(j).text.startswith(opt) Then Return frm.trackbar(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("findSlider failed.  The application list of forms changed while iterating.  Not critical.")
        End Try
        Console.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
End Module