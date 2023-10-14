import pyglet
import os 
dir_path = os.path.dirname(os.path.realpath(__file__))
window = pyglet.window.Window()
image = pyglet.image.load('../Data/corridor.jpg')

@window.event
def on_draw():
    window.clear()
    image.blit(0, 0)

pyglet.app.Run()