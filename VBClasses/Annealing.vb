Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Namespace VBClasses
    Public Class Annealing_Basics_CPP : Inherits TaskParent
        Implements IDisposable
        Public numberOfCities As Integer = 25
        Public cityPositions() As cv.Point2f
        Public cityOrder() As Integer
        Public energy As Single
        Public energyLast As Single
        Public circularPattern As Boolean = True
        Public Sub drawMap()
            dst2.SetTo(0)
            For i = 0 To cityOrder.Length - 1
                DrawCircle(dst2, cityPositions(i), atask.DotSize, white)
                dst2.Line(cityPositions(i), cityPositions(cityOrder(i)), white, atask.lineWidth, atask.lineType)
            Next
            SetTrueText("Energy" + vbCrLf + Format(energy, fmt0), New cv.Point(10, 100), 2)
        End Sub
        Public Sub setup()
            ReDim cityOrder(numberOfCities - 1)

            Dim radius = dst2.Rows * 0.45
            Dim center = New cv.Point(dst2.Cols / 2, dst2.Rows / 2)
            If circularPattern Then
                ReDim cityPositions(numberOfCities - 1)
                For i = 0 To cityPositions.Length - 1
                    Dim theta = msRNG.Next(0, 360)
                    cityPositions(i).X = radius * Math.Cos(theta) + center.X
                    cityPositions(i).Y = radius * Math.Sin(theta) + center.Y
                    cityOrder(i) = (i + 1) Mod numberOfCities
                Next
            End If
            For i = 0 To cityOrder.Length - 1
                cityOrder(i) = (i + 1) Mod numberOfCities
            Next
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim saveCityOrder = cityOrder.Clone()
            Dim hCityOrder = GCHandle.Alloc(cityOrder, GCHandleType.Pinned)
            Dim out As IntPtr = Annealing_Basics_Run(cPtr, hCityOrder.AddrOfPinnedObject, cityPositions.Length)
            hCityOrder.Free()

            Dim msg = Marshal.PtrToStringAnsi(out)
            Dim split As String() = Regex.Split(msg, "\W+")
            energy = CSng(split(split.Count - 2) + "." + split(split.Count - 1))
            If standaloneTest() Then
                If energyLast = energy Or atask.optionsChanged Then
                    Annealing_Basics_Close(cPtr)
                    setup()
                    Open()
                End If
                energyLast = energy
            End If

            drawMap()
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = Annealing_Basics_Close(cPtr)
        End Sub
    End Class








    Public Class NR_Annealing_MT_CPP : Inherits TaskParent
        Dim random As New Random_Basics
        Dim anneal() As Annealing_Basics_CPP
        Dim mats As New Mat_4to1
        Dim options As New Options_Annealing
        Dim startTime As DateTime
        Private Sub setup()
            random.options.count = options.cityCount
            random.Run(emptyMat) ' get the city positions (may or may not be used below.)

            For i = 0 To anneal.Length - 1
                anneal(i) = New Annealing_Basics_CPP()
                anneal(i).numberOfCities = options.cityCount
                anneal(i).cityPositions = random.PointList.ToArray
                anneal(i).circularPattern = options.circularFlag
                anneal(i).setup()
                anneal(i).Open() ' this will initialize the C++ copy of the city positions.
            Next
            Dim timeSpent = Now.Subtract(startTime)
            If timeSpent.TotalSeconds < 10000 Then Debug.WriteLine("time spent on last problem = " + Format(timeSpent.TotalSeconds, fmt1) + " seconds.")
            startTime = Now
        End Sub
        Public Sub New()
            ReDim anneal(Environment.ProcessorCount / 2 - 1)
            labels = {"", "", "Top 2 are best solutions, bottom 2 are worst.", "Log of Annealing progress"}
            desc = "Setup and control finding the optimal route for a traveling salesman"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If atask.optionsChanged Then setup()
            Parallel.For(0, anneal.Length,
            Sub(i)
                anneal(i).Run(src)
            End Sub)

            ' find the best result and start all the others with it.
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
            SetTrueText(strOut, New cv.Point(10, 10), 3)

            mats.mat(0) = anneal(CInt(bestList.ElementAt(0).Value)).dst2
            If bestList.Count >= 2 Then
                mats.mat(1) = anneal(CInt(bestList.ElementAt(1).Value)).dst2
                mats.mat(2) = anneal(CInt(bestList.ElementAt(bestList.Count - 2).Value)).dst2
                mats.mat(3) = anneal(CInt(bestList.ElementAt(bestList.Count - 1).Value)).dst2
            End If
            mats.Run(emptyMat)
            dst2 = mats.dst2

            ' copy the top half of the solutions to the bottom half (worst solutions)
            If options.copyBestFlag Then
                For i As Integer = 0 To anneal.Length / 2 - 1
                    anneal(bestList.ElementAt(bestList.Count - 1 - i).Value).cityOrder = anneal(bestList.ElementAt(i).Value).cityOrder
                Next
            End If


            ' if the top X are all the same energy, then we are done.
            Dim workingCount As Integer, successCounter As Integer
            For i = 0 To anneal.Count - 1
                Dim index = bestList.ElementAt(i).Value
                If anneal(index).energy <> anneal(index).energyLast Then
                    anneal(index).energyLast = anneal(index).energy
                    workingCount += 1
                Else
                    successCounter += 1
                End If
            Next
            labels(3) = $"There are {workingCount} threads working in parallel."
            If successCounter >= options.successCount Then setup()
        End Sub
    End Class
End Namespace