dirname=`cat position.conf`
fpath="./textures/$dirname/2/"
while [ 1 ]
do
    tput clear
    ls $fpath | python advancedls.py
    sleep 1s
done
