<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class InsertAlgorithm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.AlgorithmName = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.AddVB = New System.Windows.Forms.Button()
        Me.AddCSharp = New System.Windows.Forms.Button()
        Me.AddOpenGL = New System.Windows.Forms.Button()
        Me.AddCPP = New System.Windows.Forms.Button()
        Me.AddPyStream = New System.Windows.Forms.Button()
        Me.AddIncludeOnly = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'AlgorithmName
        '
        Me.AlgorithmName.Location = New System.Drawing.Point(12, 30)
        Me.AlgorithmName.Name = "AlgorithmName"
        Me.AlgorithmName.Size = New System.Drawing.Size(334, 20)
        Me.AlgorithmName.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 14)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(428, 13)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "Algorithm Name - must have the form of 'ModuleName_ClassName', i.e. RedCloud_Basi" &
    "cs"
        '
        'AddVB
        '
        Me.AddVB.Location = New System.Drawing.Point(12, 56)
        Me.AddVB.Name = "AddVB"
        Me.AddVB.Size = New System.Drawing.Size(220, 25)
        Me.AddVB.TabIndex = 5
        Me.AddVB.Text = "Add VB.Net Algorithm"
        Me.AddVB.UseVisualStyleBackColor = True
        '
        'AddCSharp
        '
        Me.AddCSharp.Location = New System.Drawing.Point(12, 149)
        Me.AddCSharp.Name = "AddCSharp"
        Me.AddCSharp.Size = New System.Drawing.Size(220, 25)
        Me.AddCSharp.TabIndex = 6
        Me.AddCSharp.Text = "Add C# Algorithm"
        Me.AddCSharp.UseVisualStyleBackColor = True
        '
        'AddOpenGL
        '
        Me.AddOpenGL.Location = New System.Drawing.Point(12, 118)
        Me.AddOpenGL.Name = "AddOpenGL"
        Me.AddOpenGL.Size = New System.Drawing.Size(220, 25)
        Me.AddOpenGL.TabIndex = 7
        Me.AddOpenGL.Text = "Add OpenGL Algorithm"
        Me.AddOpenGL.UseVisualStyleBackColor = True
        '
        'AddCPP
        '
        Me.AddCPP.Location = New System.Drawing.Point(12, 87)
        Me.AddCPP.Name = "AddCPP"
        Me.AddCPP.Size = New System.Drawing.Size(220, 25)
        Me.AddCPP.TabIndex = 8
        Me.AddCPP.Text = "Add C++ Algorithm"
        Me.AddCPP.UseVisualStyleBackColor = True
        '
        'AddPyStream
        '
        Me.AddPyStream.Location = New System.Drawing.Point(12, 180)
        Me.AddPyStream.Name = "AddPyStream"
        Me.AddPyStream.Size = New System.Drawing.Size(220, 25)
        Me.AddPyStream.TabIndex = 9
        Me.AddPyStream.Text = "Add PyStream Algorithm"
        Me.AddPyStream.UseVisualStyleBackColor = True
        '
        'AddIncludeOnly
        '
        Me.AddIncludeOnly.Location = New System.Drawing.Point(12, 211)
        Me.AddIncludeOnly.Name = "AddIncludeOnly"
        Me.AddIncludeOnly.Size = New System.Drawing.Size(220, 25)
        Me.AddIncludeOnly.TabIndex = 10
        Me.AddIncludeOnly.Text = "Add 'IncludeOnly' C++ Algorithm"
        Me.AddIncludeOnly.UseVisualStyleBackColor = True
        '
        'InsertAlgorithm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(549, 309)
        Me.Controls.Add(Me.AddIncludeOnly)
        Me.Controls.Add(Me.AddPyStream)
        Me.Controls.Add(Me.AddCPP)
        Me.Controls.Add(Me.AddOpenGL)
        Me.Controls.Add(Me.AddCSharp)
        Me.Controls.Add(Me.AddVB)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.AlgorithmName)
        Me.KeyPreview = True
        Me.Name = "InsertAlgorithm"
        Me.Text = "Add Algorithm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents AlgorithmName As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents AddVB As Button
    Friend WithEvents AddCSharp As Button
    Friend WithEvents AddOpenGL As Button
    Friend WithEvents AddCPP As Button
    Friend WithEvents AddPyStream As Button
    Friend WithEvents AddIncludeOnly As Button
End Class
