Imports cv = OpenCvSharp
' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_Basics
    Inherits VBparent
    Public i18 As New List(Of String)
    Dim inputCombo = "111,110,101,100,011,010,001,000"
    Dim cellInput(,) = {{1, 1, 1}, {1, 1, 0}, {1, 0, 1}, {1, 0, 0}, {0, 1, 1}, {0, 1, 0}, {0, 0, 1}, {0, 0, 0}}
    Public Sub New()
        initParent()
        i18.Add("00011110 Rule 30 (chaotic)")
        i18.Add("00110110 Rule 54")
        i18.Add("00111100 Rule 60")
        i18.Add("00111110 Rule 62")
        i18.Add("01011010 Rule 90")
        i18.Add("01011110 Rule 94")
        i18.Add("01100110 Rule 102")
        i18.Add("01101110 Rule 110")
        i18.Add("01111010 Rule 122")

        i18.Add("01111110 Rule 126")
        i18.Add("10010110 Rule 150")
        i18.Add("10011110 Rule 158")
        i18.Add("10110110 Rule 182")
        i18.Add("10111100 Rule 188")
        i18.Add("10111110 Rule 190")
        i18.Add("11011100 Rule 220")
        i18.Add("11011110 Rule 222")
        i18.Add("11111010 Rule 250")

        Dim label = "The 18 most interesting automata from the first 256 in 'New Kind of Science'" + vbCrLf + "The input combinations are: " + inputCombo
        combo.Setup(caller, label + vbCrLf + "output below:", i18)

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Rotate through the different rules"
            check.Box(0).Checked = True
        End If

        task.desc = "Visualize the 30 interesting examples from the first 256 in 'New Kind of Science'"
		' task.rank = 1
    End Sub
    Public Function createCells(outStr As String) As cv.Mat
        Dim outcomes(8 - 1) As Byte
        For i = 0 To outcomes.Length - 1
            outcomes(i) = Integer.Parse(outStr.Substring(i, 1))
        Next

        Dim dst = src.Clone()
        For y = 0 To dst.Height - 2
            For x = 0 To dst.Width - 2
                Dim x1 = dst.Get(Of Byte)(y, x - 1)
                Dim x2 = dst.Get(Of Byte)(y, x)
                Dim x3 = dst.Get(Of Byte)(y, x + 1)
                For i = 0 To cellInput.GetUpperBound(0) - 1
                    If x1 = cellInput(i, 0) And x2 = cellInput(i, 1) And x3 = cellInput(i, 2) Then
                        dst.Set(Of Byte)(y + 1, x, outcomes(i))
                        Exit For
                    End If
                Next
            Next
        Next
        Return dst.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Function
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            src = New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_8UC1, 0)
            src.Set(Of Byte)(0, src.Width / 2, 1)
            If task.frameCount Mod 2 Then dst2 = createCells(combo.Box.Text) Else dst1 = createCells(combo.Box.Text)
        Else
            dst1 = createCells(combo.Box.Text)
        End If
        If check.Box(0).Checked Then
            Dim index = combo.Box.SelectedIndex
            If index + 1 < i18.Count - 1 Then combo.Box.SelectedIndex += 1 Else combo.Box.SelectedIndex = 0
        End If
        label1 = combo.Box.Text
    End Sub
End Class






' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellAuto_Life
    Inherits VBparent
    Dim random As Random_Basics
    Dim grid As cv.Mat
    Dim nextgrid As cv.Mat
    Dim factor = 8
    Dim generation As Integer
    Public population As Integer
    Public nodeColor = cv.Scalar.White
    Public backColor = cv.Scalar.Black
    Private Function CountNeighbors(cellX As Integer, cellY As Integer) As Integer
        If cellX > 0 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX - 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY - 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX + 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY + 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX + 1) Then CountNeighbors += 1
        End If
        If cellX > 0 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX + 1) Then CountNeighbors += 1
        End If
        Return CountNeighbors
    End Function
    Public Sub New()
        initParent()
        grid = New cv.Mat(src.Height / factor, src.Width / factor, cv.MatType.CV_8UC1).SetTo(0)
        nextgrid = grid.Clone()

        random = New Random_Basics()
        random.rangeRect = New cv.Rect(0, 0, grid.Width, grid.Height)
        Static randomSlider = findSlider("Random Pixel Count")
        randomSlider.Value = grid.Width * grid.Height * 0.3 ' we want about 30% of cells filled.
        task.desc = "Use OpenCV to implement the Game of Life"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Static savePointCount As Integer
        Static randomSlider = findSlider("Random Pixel Count")
        If randomSlider.Value <> savePointCount Or generation = 0 Then
            random.Run()
            generation = 0
            savePointCount = randomSlider.Value
            For i = 0 To random.Points.Count - 1
                grid.Set(Of Byte)(random.Points(i).Y, random.Points(i).X, 1)
            Next
        End If
        generation += 1

        population = 0
        dst1.SetTo(backColor)
        For y = 0 To grid.Height - 1
            For x = 0 To grid.Width - 1
                Dim neighbors = CountNeighbors(x, y)
                If neighbors = 2 Or neighbors = 3 Then
                    If neighbors = 2 Then
                        nextgrid.Set(Of Byte)(y, x, grid.Get(Of Byte)(y, x))
                    Else
                        nextgrid.Set(Of Byte)(y, x, 1)
                    End If
                Else
                    nextgrid.Set(Of Byte)(y, x, 0)
                End If
                If nextgrid.Get(Of Byte)(y, x) Then
                    Dim pt = New cv.Point(x, y) * factor
                    dst1.Circle(pt, factor / 2, nodeColor, -1, task.lineType)
                    population += 1
                End If
            Next
        Next

        Static lastPopulation As Integer
        Const countInit = 200
        Static countdown As Integer = countInit
        Dim countdownText = ""
        If lastPopulation = population Then
            countdown -= 1
            countdownText = " Restart in " + CStr(countdown)
            If countdown = 0 Then
                countdownText = ""
                generation = 0
                countdown = countInit
            End If
        End If
        lastPopulation = population
        label1 = "Population " + CStr(population) + " Generation = " + CStr(generation) + countdownText
        grid = nextgrid.Clone()
    End Sub
End Class







' https://natureofcode.com/book/chapter-7-cellular-automata/
Public Class CellAuto_LifeColor
    Inherits VBparent
    Dim game As CellAuto_Life
    Public Sub New()
        initParent()
        game = New CellAuto_Life()
        game.backColor = cv.Scalar.White
        game.nodeColor = cv.Scalar.Black

        label1 = "Births are blue, deaths are red"
        task.desc = "Game of Life but with color added"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        game.Run()
        dst1 = game.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastBoard = dst1.Clone

        Dim deaths As New cv.Mat, births As New cv.Mat

        cv.Cv2.Subtract(dst1, lastBoard, births)
        cv.Cv2.Subtract(lastBoard, dst1, deaths)
        births = births.Threshold(0, 255, cv.ThresholdTypes.Binary)
        deaths = deaths.Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastBoard = dst1.Clone
        dst1 = game.dst1.Clone()
        dst1.SetTo(cv.Scalar.Blue, births)
        dst1.SetTo(cv.Scalar.Red, deaths)
    End Sub
End Class





' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellAuto_LifePopulation
    Inherits VBparent
    Dim plot As Plot_OverTime
    Dim game As CellAuto_Life
    Public Sub New()
        initParent()
        game = New CellAuto_Life()

        plot = New Plot_OverTime()
        plot.dst1 = dst2
        plot.maxScale = 2000
        plot.plotCount = 1

        task.desc = "Show Game of Life display with plot of population"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        game.Run()
        dst1 = game.dst1

        plot.plotData = New cv.Scalar(game.population, 0, 0)
        plot.Run()
        dst2 = plot.dst1
    End Sub
End Class






' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_Basics_MP
    Inherits VBparent
    Dim cell As CellAuto_Basics
    Dim i18 As New List(Of String)
    Dim i18Index As Integer
    Public Sub New()
        initParent()

        cell = New CellAuto_Basics()
        i18 = cell.i18

        task.desc = "Multi-threaded version of CellAuto_Basics"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If standalone or task.intermediateReview = caller Then
            cell.src = New cv.Mat(New cv.Size(src.Width / 4, src.Height / 4), cv.MatType.CV_8UC1, 0)
            cell.src.Set(Of Byte)(0, cell.src.Width / 2, 1)
        End If
        Parallel.For(0, 2,
          Sub(i)
              Select Case i
                  Case 0
                      label1 = i18.ElementAt(i18Index)
                      dst1 = cell.createCells(label1)
                  Case 1
                      If cell.check.Box(0).Checked Then
                          If i18Index + 1 < i18.Count - 1 Then i18Index += 1 Else i18Index = 0
                          label2 = i18.ElementAt(i18Index)
                      Else
                          If i18Index < i18.Count - 1 Then label2 = i18.ElementAt(i18Index + 1) Else label2 = i18.ElementAt(0)
                      End If
                      dst2 = cell.createCells(label2)
              End Select
          End Sub)
    End Sub
End Class







' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_All256
    Inherits VBparent
    Dim cell As CellAuto_Basics
    Public Sub New()
        initParent()
        cell = New CellAuto_Basics()
        cell.combo.Visible = False ' won't need this...

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Current Rule", 0, 255, 0)
        End If
        task.desc = "Run through all 256 combinations of outcomes"
		' task.rank = 1
    End Sub
    Private Function createOutcome(val As Integer) As String
        Dim outstr As String = ""
        For i = 0 To 8 - 1
            outstr = CStr(val Mod 2) + outstr
            val = Math.Floor(val / 2)
        Next
        Return outstr
    End Function
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim index = sliders.trackbar(0).Value
        Dim mtOn = cell.check.Box(0).Checked

        cell.src = New cv.Mat(New cv.Size(src.Width / 4, src.Height / 4), cv.MatType.CV_8UC1, 0)
        cell.src.Set(Of Byte)(0, cell.src.Width / 2, 1)

        Parallel.For(0, 2,
          Sub(i)
              Select Case i
                  Case 0
                      label1 = createOutcome(index) + " index = " + CStr(index)
                      dst1 = cell.createCells(label1)
                  Case 1
                      If mtOn = False Then Exit Sub
                      If index < 255 Then index += 1 Else index = 0
                      label2 = createOutcome(index) + " index = " + CStr(index)
                      dst2 = cell.createCells(label2)
              End Select
          End Sub)
        sliders.trackbar(0).Value = index
    End Sub
End Class





Public Class CellAuto_MultiPoint
    Inherits VBparent
    Dim cell As CellAuto_Basics
    Public Sub New()
        initParent()

        cell = New CellAuto_Basics()
        cell.combo.Box.SelectedIndex = 4 ' this one is nice...
        cell.check.Box(0).Checked = False ' just the one pattern.
        task.desc = "All256 above starts with just one point.  Here we start with multiple points."
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        cell.src = New cv.Mat(New cv.Size(src.Width / 4, src.Height / 4), cv.MatType.CV_8UC1, 0)
        Static pt1 = 0
        Static pt2 = cell.src.Width / 2
        cell.src.Set(0, pt1, 1)
        cell.src.Set(0, pt2, 1)
        cell.Run()

        dst1 = cell.dst1
        pt1 += 1
        If pt1 > cell.src.Width Then pt1 = 0
        If pt1 >= src.Width Then pt1 = 0
    End Sub
End Class

