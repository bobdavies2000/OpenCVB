Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions

Module Annealing_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Open(cityPositions As IntPtr, numberOfCities As integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Annealing_Basics_Close(saPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Run(saPtr As IntPtr, cityOrder As IntPtr, numberOfCities As integer) As IntPtr
    End Function
End Module




Public Class Annealing_Basics_CPP : Inherits VBparent
    Public numberOfCities As integer = 25
    Public restartComputation As Boolean
    Public msg As String

    Public cityPositions() As cv.Point2f
    Public cityOrder() As integer

    Public energy As Single
    Public closed As Boolean
    Public circularPattern As Boolean = True
    Dim saPtr As IntPtr
    Public Sub drawMap()
        dst1.SetTo(0)
        For i = 0 To cityOrder.Length - 1
            dst1.Circle(cityPositions(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst1.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        cv.Cv2.PutText(dst1, "Energy", New cv.Point(10, 100), task.font, task.fontSize, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        cv.Cv2.PutText(dst1, Format(energy, "#0"), New cv.Point(10, 160), task.font, task.fontSize, cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
    Public Sub setup()
        ReDim cityOrder(numberOfCities - 1)

        Dim radius = dst1.Rows * 0.45
        Dim center = New cv.Point(dst1.Cols / 2, dst1.Rows / 2)
        If circularPattern Then
            ReDim cityPositions(numberOfCities - 1)
            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 100))
            For i = 0 To cityPositions.Length - 1
                Dim theta = r.Uniform(0, 360)
                cityPositions(i).X = radius * Math.Cos(theta) + center.X
                cityPositions(i).Y = radius * Math.Sin(theta) + center.Y
                cityOrder(i) = (i + 1) Mod numberOfCities
            Next
        End If
        For i = 0 To cityOrder.Length - 1
            cityOrder(i) = (i + 1) Mod numberOfCities
        Next
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8UC3, 0)
    End Sub
    Public Sub Open()
        Dim hCityPosition = GCHandle.Alloc(cityPositions, GCHandleType.Pinned)
        saPtr = Annealing_Basics_Open(hCityPosition.AddrOfPinnedObject(), numberOfCities)
        hCityPosition.Free()
        closed = False
    End Sub
    Public Sub New()
        setup()
        task.desc = "Simulated annealing with traveling salesman.  NOTE: No guarantee simulated annealing will find the optimal solution."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If closed = True Then Exit Sub
        If standalone Or task.intermediateName = caller Then
            If task.frameCount = 0 Then
                setup()
                Open()
            End If
        End If
        Dim saveCityOrder = cityOrder.Clone()
        Dim hCityOrder = GCHandle.Alloc(cityOrder, GCHandleType.Pinned)
        Dim out As IntPtr = Annealing_Basics_Run(saPtr, hCityOrder.AddrOfPinnedObject, cityPositions.Length)
        hCityOrder.Free()
        msg = Marshal.PtrToStringAnsi(out)
        Dim split As String() = Regex.Split(msg, "\W+")
        energy = CSng(split(split.Length - 2) + "." + split(split.Length - 1))

        drawMap()

        If restartComputation Or InStr(msg, "temp=0.000") Or InStr(msg, "changesApplied=0 temp") Then
            Annealing_Basics_Close(saPtr)
            restartComputation = False
            If standalone Or task.intermediateName = caller Then
                setup()
                Open()
            End If
            closed = True
        End If
    End Sub
    Public Sub Close()
        Annealing_Basics_Close(saPtr)
    End Sub
End Class





Public Class Annealing_CPP_MT : Inherits VBparent
    Dim random As New Random_Basics
    Dim anneal() As Annealing_Basics_CPP
    Dim mats As New Mat_4to1
    Dim flow As New Font_FlowText
    Private Class CompareEnergy : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Sub setup()
        Static citySlider = findSlider("Anneal Number of Cities")
        random.countSlider.Value = citySlider.value
        random.Run(Nothing) ' get the city positions (may or may not be used below.)

        Dim numberofCities = sliders.trackbar(0).Value
        Dim circles = check.Box(2).Checked
        For i = 0 To anneal.Length - 1
            anneal(i) = New Annealing_Basics_CPP()
            anneal(i).numberOfCities = numberofCities
            anneal(i).cityPositions = random.Points2f.Clone()
            anneal(i).circularPattern = circles
            anneal(i).setup()
            anneal(i).cityPositions = anneal(0).cityPositions.Clone() ' duplicate for all threads - working on the same set of points.
            anneal(i).Open() ' this will initialize the C++ copy of the city positions.
        Next
        Static startTime As DateTime
        Dim timeSpent = Now.Subtract(startTime)
        If timeSpent.TotalSeconds < 10000 Then Console.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, "#0.0") + " seconds.")
        startTime = Now
    End Sub

    Public Sub New()
        ReDim anneal(Environment.ProcessorCount - 1)
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Anneal Number of Cities", 5, 500, 25)
            sliders.setupTrackBar(1, "Success = top X threads agree on energy level.", 2, anneal.Count, anneal.Count)
        End If
        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Restart TravelingSalesman"
            check.Box(1).Text = "Copy Best Intermediate solutions (top half) to Bottom Half"
            check.Box(2).Text = "Circular pattern of cities (allows you to visually check if successful.)"
            check.Box(1).Checked = True
            check.Box(2).Checked = True
        End If

        label1 = "Log of Annealing progress"
        label2 = "Top 2 are best solutions, bottom 2 are worst."

        task.desc = "Setup and control finding the optimal route for a traveling salesman"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static CityCountSlider = findSlider("Anneal Number of Cities")
        Static restartCheck = findCheckBox("Restart TravelingSalesman")
        Static copyBestCheck = findCheckBox("Copy Best Intermediate solutions (top half) to Bottom Half")
        Static circularCheck = findCheckBox("Circular pattern of cities (allows you to visually check if successful.)")
        If anneal(0) Is Nothing Then setup() ' setup here rather than in algorithm so all threads work on the same problem.
        If anneal(0).numberOfCities <> CityCountSlider.Value Or restartCheck.Checked Or circularCheck.Checked <> anneal(0).circularPattern Then setup()
        restartCheck.Checked = False
        Dim allClosed As Boolean = True
        Parallel.For(0, anneal.Length,
            Sub(i)
                If anneal(i).closed = False Then
                    anneal(i).Run(src)
                    allClosed = False
                End If
            End Sub)

        ' find the best result and start all the others with it.
        Dim minEnergy As Single = Single.MaxValue
        Dim minIndex As Integer = 0
        Dim bestList As New SortedList(Of Single, Integer)(New CompareEnergy)
        flow.msgs.Clear()
        For i = 0 To anneal.Length - 1
            bestList.Add(anneal(i).energy, i)
            flow.msgs.Add("CPU=" + Format(i, "00") + " " + anneal(i).msg)
        Next
        flow.Run(Nothing)

        ' if the top 4 are all the same energy, then we are done.
        If bestList.Count > 1 Then
            Dim sameEnergy As Integer = 1
            Dim successCounter = sliders.trackbar(1).Value
            For i = 1 To successCounter - 1
                If anneal(CInt(bestList.ElementAt(i).Value)).energy = anneal(CInt(bestList.ElementAt(0).Value)).energy Then sameEnergy += 1
            Next
            If sameEnergy = successCounter Then allClosed = True
            If sameEnergy = 1 Then
                label1 = "There is only " + CStr(sameEnergy) + " thread at the best energy level."
            Else
                label1 = "There are " + CStr(sameEnergy) + " threads at the best energy level."
            End If
        Else
            label1 = "Energy level is " + CStr(anneal(0).energy)
        End If

        mats.mat(0) = anneal(CInt(bestList.ElementAt(0).Value)).dst1
        If bestList.Count >= 2 Then
            mats.mat(1) = anneal(CInt(bestList.ElementAt(1).Value)).dst1
            mats.mat(2) = anneal(CInt(bestList.ElementAt(bestList.Count - 2).Value)).dst1
            mats.mat(3) = anneal(CInt(bestList.ElementAt(bestList.Count - 1).Value)).dst1
        End If
        mats.Run(src)
        dst2 = mats.dst1

        ' copy the top half of the solutions to the bottom half (worst solutions)
        If copyBestCheck.Checked Then
            For i = 0 To anneal.Length / 2 - 1
                anneal(bestList.ElementAt(bestList.Count - 1 - i).Value).cityOrder = anneal(bestList.ElementAt(i).Value).cityOrder
            Next
        End If

        If allClosed Then setup()
    End Sub
End Class




Public Class Annealing_Options : Inherits VBparent
    Dim random As New Random_Basics
    Public anneal As Annealing_Basics_CPP
    Dim flow As New Font_FlowText
    Public Sub New()
        random.Run(Nothing) ' get the city positions (may or may not be used below.)

        If check.Setup(caller, 2) Then
            check.Box(0).Text = "Restart TravelingSalesman"
            check.Box(1).Text = "Circular pattern of cities (allows you to visually check if successful.)"
            check.Box(1).Checked = True
        End If

        label1 = "Log of Annealing progress"

        Const initialCities = 25
        findSlider("Random Pixel Count").Value = initialCities ' change the default number of cities here.
        anneal = New Annealing_Basics_CPP()
        anneal.numberOfCities = initialCities
        anneal.setup()
        anneal.Open()
        task.desc = "Setup and control finding the optimal route for a traveling salesman"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static randomSlider = findSlider("Random Pixel Count")
        Dim numberOfCities = randomSlider.Value
        Dim circularPattern = check.Box(1).Checked ' do they want a circular pattern?
        If numberOfCities <> anneal.numberOfCities Or circularPattern <> anneal.circularPattern Then
            anneal.circularPattern = circularPattern
            anneal.numberOfCities = numberOfCities
            anneal.restartComputation = True
        Else
            anneal.restartComputation = check.Box(0).Checked
            check.Box(0).Checked = False
        End If

        anneal.Run(src)
        dst2 = anneal.dst1

        If anneal.restartComputation Then
            anneal.restartComputation = False
            random.Run(Nothing) ' get the city positions (may or may not be used below.)
            If check.Box(1).Checked = False Then anneal.cityPositions = random.Points2f.Clone()
            anneal.setup()
            anneal.Open()
            Static startTime As DateTime
            Dim timeSpent = Now.Subtract(startTime)
            If timeSpent.TotalSeconds < 10000 Then Console.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, "#0.0") + " seconds.")
            startTime = Now
        End If

        flow.msgs.Add(anneal.msg)
        flow.Run(Nothing)

    End Sub
End Class


