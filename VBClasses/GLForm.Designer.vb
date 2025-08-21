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
        GLControl = New SharpGL.OpenGLControl()
        CType(GLControl, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' GLControl
        ' 
        GLControl.DrawFPS = False
        GLControl.FrameRate = 30
        GLControl.Location = New Point(19, 20)
        GLControl.Margin = New Padding(6, 5, 6, 5)
        GLControl.Name = "GLControl"
        GLControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1
        GLControl.RenderContextType = SharpGL.RenderContextType.FBO
        GLControl.RenderTrigger = SharpGL.RenderTrigger.TimerBased
        GLControl.Size = New Size(1106, 710)
        GLControl.TabIndex = 0
        ' 
        ' sgl
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1143, 750)
        Controls.Add(GLControl)
        Margin = New Padding(4, 5, 4, 5)
        Name = "sgl"
        Text = "sgl"
        CType(GLControl, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents GLControl As SharpGL.OpenGLControl
End Class
