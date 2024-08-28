# https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
#OpenCV Motion Detector
#@author: methylDragon
#                                   .     .
#                                .  |\-^-/|  .
#                               /| } O.=.O { |\
#                              /´ \ \_ ~ _/ / `\
#                            /´ |  \-/ ~ \-/  | `\
#                            |   |  /\\ //\  |   |
#                             \|\|\/-""-""-\/|/|/
#                                     ______/ /
#                                     '------'
#                       _   _        _  ___
#             _ __  ___| |_| |_ _  _| ||   \ _ _ __ _ __ _ ___ _ _
#            | '  \/ -_)  _| ' \ || | || |) | '_/ _` / _` / _ \ ' \
#            |_|_|_\___|\__|_||_\_, |_||___/|_| \__,_\__, \___/_||_|
#                               |__/                 |___/
#            -------------------------------------------------------
#                           github.com/methylDragon
#References/Adapted From:
#https://www.pyimagesearch.com/2015/05/25/basic-motion-detection-and-tracking-with-python-and-opencv/
#Description:
#This script runs a motion detector! It detects transient motion in a room
#and said movement is large enough, and recent enough, reports that there is
#motion!
#Run the script with a working webcam! You'll see how it works!

import cv2 as cv
import numpy as np
from PyStream import PyStreamRun

titleWindow = 'MotionDetector_PS.py'

cv.namedWindow(titleWindow)

# =============================================================================
# USER-SET PARAMETERS
# =============================================================================

# Number of frames to pass before changing the frame to compare the current
# frame against
FRAMES_TO_PERSIST = 10

# Minimum boxed area for a detected motion to count as actual motion
# Use to filter out noise or small objects
MIN_SIZE_FOR_MOVEMENT = 2000

# Minimum length of time where no motion is detected it should take
#(in program cycles) for the program to declare that there is no movement
MOVEMENT_DETECTED_PERSISTENCE = 100

# =============================================================================
# CORE PROGRAM
# =============================================================================
def OpenCVCode(imgRGB, depth32f, frameCount):
    global first_frame, next_frame, font, delay_counter, movement_persistent_counter
    # Set transient motion detected as false
    transient_movement_flag = False
    
    text = "Unoccupied"
    # Resize and save a greyscale version of the image
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)

    # Blur it to remove camera noise (reducing false positives)
    gray = cv.GaussianBlur(gray, (21, 21), 0)

    # If the first frame is nothing, initialise it
    if frameCount == 0: 
        first_frame = gray    

    delay_counter += 1

    # Otherwise, set the first frame to compare as the previous frame
    # But only if the counter reaches the appriopriate value
    # The delay is to allow relatively slow motions to be counted as large
    # motions if they're spread out far enough
    if delay_counter > FRAMES_TO_PERSIST:
        delay_counter = 0
        first_frame = next_frame

        
    # Set the next frame to compare (the current frame)
    next_frame = gray

    # Compare the two frames, find the difference
    frame_delta = cv.absdiff(first_frame, next_frame)
    thresh = cv.threshold(frame_delta, 25, 255, cv.THRESH_BINARY)[1]

    # Fill in holes via dilate(), and find contours of the thesholds
    thresh = cv.dilate(thresh, None, iterations = 2)
    cnts, _ = cv.findContours(thresh.copy(), cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)

    # loop over the contours
    for c in cnts:

        # Save the coordinates of all found contours
        (x, y, w, h) = cv.boundingRect(c)
        
        # If the contour is too small, ignore it, otherwise, there's transient
        # movement
        if cv.contourArea(c) > MIN_SIZE_FOR_MOVEMENT:
            transient_movement_flag = True
            
            # Draw a rectangle around big enough movements
            cv.rectangle(imgRGB, (x, y), (x + w, y + h), (0, 255, 0), 2)

    # The moment something moves momentarily, reset the persistent
    # movement timer.
    if transient_movement_flag == True:
        movement_persistent_flag = True
        movement_persistent_counter = MOVEMENT_DETECTED_PERSISTENCE

    # As long as there was a recent transient movement, say a movement
    # was detected    
    if movement_persistent_counter > 0:
        text = "Movement Detected " + str(movement_persistent_counter)
        movement_persistent_counter -= 1
    else:
        text = "No Movement Detected"

    # Print the text on the screen, and display the raw and processed video 
    # feeds
    cv.putText(imgRGB, str(text), (10,35), font, 0.75, (255,255,255), 2, cv.LINE_AA)
    
    # For if you want to show the individual video frames
    #    cv.imshow(titleWindow, imgRGB)
    #    cv.imshow("delta", frame_delta)
    
    # Convert the frame_delta to color for splicing
    frame_delta = cv.cvtColor(frame_delta, cv.COLOR_GRAY2BGR)

    (h,w) = imgRGB.shape[:2] # original image size

    delta = cv.resize(frame_delta, (int(w / 2), int(h / 2)))
    img = cv.resize(imgRGB, (int(w / 2), int(h / 2)))
    # Splice the two video frames together to make one long horizontal one
    cv.imshow(titleWindow,cv.hconcat([delta, img]))
    frameCount += 1
    return imgRGB, None

movement_persistent_counter = 0
font = cv.FONT_HERSHEY_SIMPLEX
delay_counter = 0
PyStreamRun(OpenCVCode, titleWindow)