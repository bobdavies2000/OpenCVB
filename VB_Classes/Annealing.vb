Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class Annealing_Basics_CPP : Inherits VB_Algorithm
    Public numberOfCities As Integer = 25
    Public cityPositions() As cv.Point2f
    Public cityOrder() As Integer
    Public energy As Single
    Public energyLast As Single
    Public circularPattern As Boolean = True
    Public Sub drawMap()
        dst2.SetTo(0)
        For i = 0 To cityOrder.Length - 1
            dst2.Circle(cityPositions(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst2.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        setTrueText("Energy" + vbCrLf + Format(energy, fmt0), New cv.Point(10, 100), 2)
    End Sub
    Public Sub setup()
        ReDim cityOrder(numberOfCities - 1)

        Dim radius = dst2.Rows * 0.45
        Dim center = New cv.Point(dst2.Cols / 2, dst2.Rows / 2)
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
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, 0)
    End Sub
    Public Sub Open()
        Dim hCityPosition = GCHandle.Alloc(cityPositions, GCHandleType.Pinned)
        cPtr = Annealing_Basics_Open(hCityPosition.AddrOfPinnedObject(), numberOfCities)
        hCityPosition.Free()
    End Sub
    Public Sub New()
        energy = -1
        setup()
        Open()
        desc = "Simulated annealing with traveling salesman.  NOTE: No guarantee simulated annealing will find the optimal solution."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim saveCityOrder = cityOrder.Clone()
        Dim hCityOrder = GCHandle.Alloc(cityOrder, GCHandleType.Pinned)
        Dim out As IntPtr = Annealing_Basics_Run(cPtr, hCityOrder.AddrOfPinnedObject, cityPositions.Length)
        hCityOrder.Free()

        Dim msg = Marshal.PtrToStringAnsi(out)
        Dim split As String() = Regex.Split(msg, "\W+")
        energy = CSng(split(split.Count - 2) + "." + split(split.Count - 1))
        If standalone Then
            If energyLast = energy Or task.optionsChanged Then
                Annealing_Basics_Close(cPtr)
                setup()
                Open()
            End If
            energyLast = energy
        End If

        drawMap()
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Annealing_Basics_Close(cPtr)
    End Sub
End Class








Module Annealing_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Open(cityPositions As IntPtr, numberOfCities As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Close(saPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Annealing_Basics_Run(saPtr As IntPtr, cityOrder As IntPtr, numberOfCities As Integer) As IntPtr
    End Function
End Module









Public Class Annealing_MultiThreaded : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Dim anneal() As Annealing_Basics_CPP
    Dim mats As New Mat_4to1
    Dim options As New Options_Annealing
    Private Sub setup()
        random.options.countSlider.Value = options.cityCount
        random.Run(Nothing) ' get the city positions (may or may not be used below.)

        For i = 0 To anneal.Length - 1
            anneal(i) = New Annealing_Basics_CPP()
            anneal(i).numberOfCities = options.cityCount
            anneal(i).cityPositions = random.PointList.ToArray
            anneal(i).circularPattern = options.circularFlag
            anneal(i).setup()
            anneal(i).cityPositions = anneal(0).cityPositions.Clone() ' duplicate for all threads - working on the same set of points.
            anneal(i).Open() ' this will initialize the C++ copy of the city positions.
        Next
        Static startTime As DateTime
        Dim timeSpent = Now.Subtract(startTime)
        If timeSpent.TotalSeconds < 10000 Then Console.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, fmt1) + " seconds.")
        startTime = Now
    End Sub
    Public Sub New()
        ReDim anneal(Environment.ProcessorCount / 2 - 1)
        labels = {"", "", "Top 2 are best solutions, bottom 2 are worst.", "Log of Annealing progress"}
        desc = "Setup and control finding the optimal route for a traveling salesman"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()
        If task.optionsChanged Then setup()
        Parallel.For(0, anneal.Length,
            Sub(i)
                anneal(i).Run(src)
            End Sub)

        ' find the best result and start all the others with it.
        Dim minEnergy As Single = Single.MaxValue
        Dim minIndex As Integer = 0
        Dim bestList As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        strOut = ""
        For i = 0 To anneal.Length - 1
            bestList.Add(anneal(i).energy, i)
            If i Mod 2 = 0 Then
                strOut += "CPU=" + Format(i, "00") + " energy=" + Format(anneal(i).energy, "0") + vbTab
            Else
                strOut += "CPU=" + Format(i, "00") + " energy=" + Format(anneal(i).energy, "0") + vbCrLf
            End If
        Next
        setTrueText(strOut, New cv.Point(10, 10), 3)

        mats.mat(0) = anneal(CInt(bestList.ElementAt(0).Value)).dst2
        If bestList.Count >= 2 Then
            mats.mat(1) = anneal(CInt(bestList.ElementAt(1).Value)).dst2
            mats.mat(2) = anneal(CInt(bestList.ElementAt(bestList.Count - 2).Value)).dst2
            mats.mat(3) = anneal(CInt(bestList.ElementAt(bestList.Count - 1).Value)).dst2
        End If
        mats.Run(Nothing)
        dst2 = mats.dst2

        ' copy the top half of the solutions to the bottom half (worst solutions)
        If options.copyBestFlag Then
            For i = 0 To anneal.Length / 2 - 1
                anneal(bestList.ElementAt(bestList.Count - 1 - i).Value).cityOrder = anneal(bestList.ElementAt(i).Value).cityOrder
            Next
        End If


        ' if the top X are all the same energy, then we are done.
        Dim successCounter As Integer
        For i = 0 To anneal.Count - 1
            Dim index = bestList.ElementAt(i).Value
            If anneal(index).energy = anneal(index).energyLast Then successCounter += 1 Else anneal(index).energyLast = anneal(index).energy
        Next
        labels(3) = "There are " + CStr(successCounter) + " threads completed."

        If successCounter >= options.successCount Then setup()
    End Sub
End Class