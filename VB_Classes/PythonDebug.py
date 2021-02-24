import tarfile
import os
os.chdir("C:\_src\OpenCVB\Data/")
tar = tarfile.open("ibug_300W_large_face_landmark_dataset.tar.gz")
tar.extractall()
tar.close