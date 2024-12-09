Imports cvb = OpenCvSharp
' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellularAutomata_Basics : Inherits TaskParent
    Public i18 As New List(Of String)({"00011110 Rule 30 (chaotic)", "00110110 Rule 54", "00111100 Rule 60", "00111110 Rule 62",
                                       "01011010 Rule 90", "01011110 Rule 94", "01100110 Rule 102", "01101110 Rule 110",
                                       "01111010 Rule 122", "01111110 Rule 126", "10010110 Rule 150", "10011110 Rule 158",
                                       "10110110 Rule 182", "10111100 Rule 188", "10111110 Rule 190", "11011100 Rule 220",
                                       "11011110 Rule 222", "11111010 Rule 250"})
    Dim inputCombo = "111,110,101,100,011,010,001,000"
    Dim cellInput(,) = {{1, 1, 1}, {1, 1, 0}, {1, 0, 1}, {1, 0, 0}, {0, 1, 1}, {0, 1, 0}, {0, 0, 1}, {0, 0, 0}}
    Public options As New Options_CellAutomata
    Public input As New cvb.Mat
    Public index As Integer
    Public Sub New()
        Dim label = "The 18 most interesting automata from the first 256 in 'New Kind of Science'" + vbCrLf + "The input combinations are: " + inputCombo
        desc = "Visualize the 30 interesting examples from the first 256 in 'New Kind of Science'"
    End Sub
    Public Function createCells(outStr As String) As cvb.Mat
        Dim outcomes(7) As Byte
        For i = 0 To outcomes.Length - 1
            outcomes(i) = Integer.Parse(outStr.Substring(i, 1))
        Next

        Dim dst = input.Clone()
        For y = 0 To dst.Height - 2
            For x = 0 To dst.Width - 2
                Dim x1 = dst.Get(Of Byte)(y, x - 1)
                Dim x2 = dst.Get(Of Byte)(y, x)
                Dim x3 = dst.Get(Of Byte)(y, x + 1)
                For i = 0 To cellInput.GetUpperBound(0)
                    If x1 = cellInput(i, 0) And x2 = cellInput(i, 1) And x3 = cellInput(i, 2) Then
                        dst.Set(Of Byte)(y + 1, x, outcomes(i))
                        Exit For
                    End If
                Next
            Next
        Next
        Return dst.ConvertScaleAbs(255).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.heartBeat Then
            labels(2) = i18(index)
            index += 1
            If index >= i18.Count Then index = 0
        End If

        If standalone Then
            input = New cvb.Mat(New cvb.Size(src.Width, src.Height), cvb.MatType.CV_8UC1, 0)
            input.Set(Of Byte)(0, src.Width / 2, 1)
            dst2 = createCells(labels(2))
        Else
            input = src.Clone
            dst2 = createCells(labels(2))
        End If
    End Sub
End Class






' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellularAutomata_Life : Inherits TaskParent
    Dim random As New Random_Basics
    Dim grid As cvb.Mat
    Dim nextgrid As cvb.Mat
    Dim factor = 8
    Dim age As Integer
    Public population As Integer
    Public nodeColor = cvb.Scalar.White
    Public backColor = cvb.Scalar.Black
    Dim savePointCount As Integer
    Dim lastPopulation As Integer
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
        grid = New cvb.Mat(dst2.Height / factor, dst2.Width / factor, cvb.MatType.CV_8UC1).SetTo(0)
        nextgrid = grid.Clone()
        random.range = New cvb.Rect(0, 0, grid.Width, grid.Height)
        FindSlider("Random Pixel Count").Value = grid.Width * grid.Height * 0.3 ' we want about 30% of cells filled.
        desc = "Use OpenCV to implement the Game of Life"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If random.options.count <> savePointCount Or age = 0 Then
            random.Run(empty)
            age = 0
            savePointCount = random.options.count
            For i = 0 To random.PointList.Count - 1
                grid.Set(Of Byte)(random.PointList(i).Y, random.PointList(i).X, 1)
            Next
        End If
        age += 1

        population = 0
        dst2.SetTo(backColor)
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
                    Dim pt = New cvb.Point(x, y) * factor
                    DrawCircle(dst2, pt, factor / 2, nodeColor)
                    population += 1
                End If
            Next
        Next

        Const countInit = 200
        Static countdown As Integer = countInit
        Dim countdownText = ""
        If lastPopulation = population Then
            countdown -= 1
            countdownText = " Restart in " + CStr(countdown)
            If countdown = 0 Then
                countdownText = ""
                age = 0
                countdown = countInit
            End If
        End If
        lastPopulation = population
        labels(2) = "Population " + CStr(population) + " Generation = " + CStr(age) + countdownText
        grid = nextgrid.Clone()
    End Sub
End Class







' https://natureofcode.com/book/chapter-7-cellular-automata/
Public Class CellularAutomata_LifeColor : Inherits TaskParent
    Dim game As New CellularAutomata_Life
    Public Sub New()
        game.backColor = white
        game.nodeColor = cvb.Scalar.Black

        labels(2) = "Births are blue, deaths are red"
        desc = "Game of Life but with color added"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim lastBoard = game.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        game.Run(src)
        dst1 = game.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim deaths As New cvb.Mat, births As New cvb.Mat

        cvb.Cv2.Subtract(dst1, lastBoard, births)
        cvb.Cv2.Subtract(lastBoard, dst1, deaths)
        births = births.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        deaths = deaths.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        dst2 = game.dst2.Clone()
        dst2.SetTo(cvb.Scalar.Blue, births)
        dst2.SetTo(cvb.Scalar.Red, deaths)
    End Sub
End Class





' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellularAutomata_LifePopulation : Inherits TaskParent
    Dim plot As New Plot_OverTimeSingle
    Dim game As New CellularAutomata_Life
    Public Sub New()
        desc = "Show Game of Life display with plot of population"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        game.Run(src)
        dst2 = game.dst2

        plot.plotData = New cvb.Scalar(game.population, 0, 0)
        plot.Run(empty)
        dst3 = plot.dst2
    End Sub
End Class






Public Class CellularAutomata_MultiPoint : Inherits TaskParent
    Dim cell As New CellularAutomata_Basics
    Dim val1 As Integer = 0
    Dim val2 As Integer = dst2.Width / 2
    Public Sub New()
        cell.index = 4 ' this one is nice...
        desc = "All256 above starts with just one point.  Here we start with multiple points."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim tmp = New cvb.Mat(New cvb.Size(src.Width / 4, src.Height / 4), cvb.MatType.CV_8UC1, 0)
        tmp.Set(0, val1, 1)
        tmp.Set(0, val2, 1)
        cell.Run(tmp)

        dst2 = cell.dst2
        val1 += 1
        If val1 > tmp.Width Then val1 = 0
        If val2 >= src.Width Then val2 = 0
    End Sub
End Class






' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellularAutomata_All256 : Inherits TaskParent
    Dim cell As New CellularAutomata_Basics
    Dim options As New Options_CellAutomata
    Dim ruleSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        ruleSlider = FindSlider("Current Rule")
        desc = "Run through all 256 combinations of outcomes"
    End Sub
    Private Function createOutcome(val As Integer) As String
        Dim outstr As String = ""
        For i = 0 To 8 - 1
            outstr = CStr(val Mod 2) + outstr
            val = Math.Floor(val / 2)
        Next
        Return outstr
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            cell.input = New cvb.Mat(New cvb.Size(src.Width / 4, src.Height / 4), cvb.MatType.CV_8UC1, 0)
            cell.input.Set(Of Byte)(0, cell.input.Width / 2, 1)

            labels(2) = createOutcome(options.currentRule) + " options.currentRule = " + CStr(options.currentRule)
            dst2 = cell.createCells(labels(2))

            options.RunOpt()

            labels(3) = createOutcome(options.currentRule) + " current rule = " + CStr(options.currentRule)
            dst3 = cell.createCells(labels(3))

            If ruleSlider.Value < ruleSlider.Maximum - 1 Then ruleSlider.Value += 1 Else ruleSlider.Value = 0
        End If
    End Sub
End Class

