Imports cv = OpenCvSharp
Imports System.Threading
' https://github.com/nemanja-m/gaps
Public Class Puzzle_Basics : Inherits VB_Algorithm
    Public scrambled As New List(Of cv.Rect) ' this is every roi regardless of size.
    Public unscrambled As New List(Of cv.Rect) ' this is every roi regardless of size.
    Public image As New cv.Mat
    Public Sub New()
        desc = "Create the puzzle pieces to solve with correlation."
    End Sub
    Function Shuffle(Of T)(collection As IEnumerable(Of T)) As List(Of T)
        Dim r As Random = New Random()
        Shuffle = collection.OrderBy(Function(a) r.Next()).ToList()
    End Function
    Public Sub RunVB(src as cv.Mat)
        unscrambled.Clear()
        Dim inputROI As New List(Of cv.Rect)
        For j = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(j)
            If roi.Width = gOptions.GridSize.Value And roi.Height = gOptions.GridSize.Value Then inputROI.Add(task.gridList(j))
        Next

        scrambled = Shuffle(inputROI)
        image = src.Clone

        ' display image with shuffled roi's
        For i = 0 To scrambled.Count - 1
            Dim roi = task.gridList(i)
            Dim roi2 = scrambled(i)
            If roi.Width = gOptions.GridSize.Value And roi.Height = gOptions.GridSize.Value And
               roi2.Width = gOptions.GridSize.Value And roi2.Height = gOptions.GridSize.Value Then dst2(roi2) = src(roi)
        Next
    End Sub
End Class








Public Class Puzzle_Solver : Inherits VB_Algorithm
    Public puzzle As New Puzzle_Basics
    Dim solution As New List(Of cv.Rect)
    Dim match As New Match_Basics
    Public grayMat As cv.Mat
    Public Sub New()
        If standaloneTest() Then gOptions.GridSize.Value = 8
        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Start another puzzle")
            check.Box(0).Checked = True
        End If

        labels = {"", "", "Puzzle Input", "Puzzle Solver Output - missing pieces can result from identical cells (usually bright white)"}
        desc = "Solve the puzzle using matchTemplate"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static startBox = findCheckBox("Start another puzzle")
        Static puzzleIndex As Integer
        If task.optionsChanged Or startBox.checked Then
            startBox.checked = False
            puzzle.Run(src)
            dst2 = puzzle.dst2
            dst3.SetTo(0)
            grayMat = puzzle.image.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            puzzleIndex = 0
        End If

        If puzzle.scrambled.Count > puzzle.unscrambled.Count Then
            ' find one piece of the puzzle on each iteration.
            Dim rect = puzzle.scrambled(puzzleIndex)
            match.template = grayMat(rect)
            match.Run(grayMat)
            Dim maxloc = New cv.Point2f(match.drawRect.X, match.drawRect.Y)
            Dim bestRect = New cv.Rect(maxloc.X, maxloc.Y, rect.Width, rect.Height)
            puzzle.unscrambled.Add(bestRect)
            puzzleIndex += 1
            dst3(bestRect) = puzzle.image(bestRect)
        End If
    End Sub
End Class








Public Class Puzzle_SolverDynamic : Inherits VB_Algorithm
    Dim puzzle As New Puzzle_Solver
    Public Sub New()
        If standaloneTest() Then gOptions.GridSize.Value = 8
        labels = {"", "", "Latest Puzzle input image", "Puzzle Solver Output - missing pieces can occur because of motion or when cells are identical."}
        desc = "Instead of matching the original image as Puzzle_Solver, match the latest image from the camera."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        puzzle.puzzle.image = src.Clone
        puzzle.grayMat = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        puzzle.Run(src)
        dst2 = puzzle.dst2
        dst3 = puzzle.dst3
    End Sub
End Class