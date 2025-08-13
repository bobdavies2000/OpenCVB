<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class sgl
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
        OpenglControl1 = New SharpGL.OpenGLControl()
        CType(OpenglControl1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' OpenglControl1
        ' 
        OpenglControl1.DrawFPS = True
        OpenglControl1.FrameRate = 60
        OpenglControl1.Location = New Point(13, 12)
        OpenglControl1.Margin = New Padding(4, 3, 4, 3)
        OpenglControl1.Name = "OpenglControl1"
        OpenglControl1.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1
        OpenglControl1.RenderContextType = SharpGL.RenderContextType.FBO
        OpenglControl1.RenderTrigger = SharpGL.RenderTrigger.TimerBased
        OpenglControl1.Size = New Size(774, 426)
        OpenglControl1.TabIndex = 0
        ' 
        ' sgl
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(OpenglControl1)
        Name = "sgl"
        Text = "sgl"
        CType(OpenglControl1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents OpenglControl1 As SharpGL.OpenGLControl
End Class
