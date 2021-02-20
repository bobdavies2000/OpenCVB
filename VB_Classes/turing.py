'''
Multiscale Turing Patterns generator
====================================

Inspired by http://www.jonathanmccabe.com/Cyclic_Symmetric_Multi-Scale_Turing_Patterns.pdf
'''
import sys
import numpy as np
import cv2 as cv
from common import draw_str
import getopt, sys
from itertools import count
titleWindow = "Turing.py"

help_message = '''
USAGE: turing.py [-o <output.avi>]
'''

def main():
    print(help_message)

    w, h = 512, 512

    args, args_list = getopt.getopt(sys.argv[1:], 'o:', [])
    args = dict(args)
    out = None
    if '-o' in args:
        fn = args['-o']
        out = cv.VideoWriter(args['-o'], cv.VideoWriter_fourcc(*'DIB '), 30.0, (w, h), False)
        print('writing %s ...' % fn)

    turing = np.zeros((h, w), np.float32)
    cv.randu(turing, np.array([0]), np.array([1]))

    def process_scale(a_lods, lod):
        d = a_lods[lod] - cv.pyrUp(a_lods[lod+1])
        for _i in range(lod):
            d = cv.pyrUp(d)
        v = cv.GaussianBlur(d*d, (3, 3), 0)
        return np.sign(d), v

    scale_num = 6
    for frame_i in count():
        a_lods = [turing]
        for i in range(scale_num):
            a_lods.append(cv.pyrDown(a_lods[-1]))
        ms, vs = [], []
        for i in range(1, scale_num):
            m, v = process_scale(a_lods, i)
            ms.append(m)
            vs.append(v)
        mi = np.argmin(vs, 0)
        turing += np.choose(mi, ms) * 0.025
        turing = (turing-turing.min()) / turing.ptp()

        if out:
            out.write(turing)
        vis = turing.copy()
        draw_str(vis, (20, 20), 'frame %d' % frame_i)
        cv.imshow(titleWindow, vis)
        if cv.waitKey(5) == 27:
            break

if __name__ == '__main__':
    print(__doc__)
    main()
