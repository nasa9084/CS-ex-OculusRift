#-*- coding:utf-8 -*-
import sys
import time
import os

basechar = " "
donechar = "@"
poschar  = "-"
yawmag = 2
pitchmag = 1
minangle = -90
maxangle = 90
vbottom = (minangle + 180) * pitchmag
vtop = (maxangle + 180) * pitchmag
width = 360*yawmag

def advancedls():
        ret = sys.stdin.read().split()
	with open("position.conf", "r") as f:
		for line in f:
			posname = line
        path = "textures/"
        pospath = path + posname + "/2/"
        ls = []

        with open("CurrentPosition.txt", 'r') as f:
                for line in f:
                        (px, py) = (int(x) for x in line.split(","))

        txts = []

        with open(posname + 'filelist.txt', 'r') as f:
                for line in f:
                        txts.append(line)

        for fname in ret:
	        d = fname.split("_")[1:]
	        d[1] = d[1][:-4]
	        d = [int(x) for x in d]
	        ls.append(d)

        for pos in ls:
                txts[pos[1]] = txts[pos[1]][:pos[0]] + donechar + txts[pos[1]][pos[0]+1:]

        # 描画部
        for i,v in enumerate(reversed(txts)):
                if vbottom<i<vtop:
                        if i!=(len(txts)-py-1):
                                line = (v[:px] + poschar + v[px+1:])[::-1].replace("\n", "").replace("\r", "")
                                line = basechar*20 + line + basechar*20
                                print line
                        else:
                                line = poschar*20
                                for i in range(width,0,-1):
                                        if v[i]==donechar:
                                                line += v[i]
                                        else:
                                                line += poschar
                                line += poschar*20
                                print line
                elif vtop<i:
                        break

        with open(posname + 'filelist.txt', 'w') as f:
                for line in txts:
                        f.write(line)

        for fname in ret:
                os.system("rm ./" + path + posname + "/2/" + fname)

def ls():
        ret = sys.stdin.read().split()
        ls = []

    	try:
    		for line in open('CurrentPosition.txt', 'r'):
        		(px,py) = map(int,line.split(","))
                        #(px, py) = [int(x) for x in line.split(",")]
        except:
        	sys.exit(1)

        for fname in ret:
	        d = fname.split("_")[1:]
	        d[1] = d[1][:-4]
	        d = [int(x) for x in d]
	        ls.append(d)

        for v in range(vtop,vbottom,-1):
	        p = ""
	        for h in range(360*pitchmag,0,-1):
		        p += "+" if [h,v] in ls else " " if v!=py and h!=px else "-"

	        print p

def make():
        string = basechar*361*yawmag
	with open("position.conf", "r") as f:
		for line in f:
			posname = line
        with open(posname + "filelist.txt", "w") as f:
                for i in range(0, 361*pitchmag):
                        f.write(string+"\n")

if __name__=="__main__":
        cmdparam = sys.argv
        if len(cmdparam)==1:
                advancedls()
        else:
                make()
