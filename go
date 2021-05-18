set -e

mkdir -p new
../tools/acme -o new/jsw1 -r jsw1.txt --vicelabels jsw1.sym jsw1.a
../tools/acme -o new/jsw1.5 -r jsw1.5.txt --vicelabels jsw1.5.sym jsw1.5.a

sort -o jsw1.tmp jsw1.sym
uniq jsw1.tmp jsw1.sym
rm jsw1.tmp

sort -o jsw1.5.tmp jsw1.5.sym
uniq jsw1.5.tmp jsw1.5.sym
rm jsw1.5.tmp

run="1"
skipdiff="0"
for var in "$@"
do
    if [ "$var" == "norun" ]; then
        run="0"
    fi
done

../tools/tokenizer2 /Users/tobynelson/Code/6502/jsw2021/new/loader.txt >new/loader

printf "*BASIC\rPAGE=&1900\r*FX21\rCLOSE#0:CHAIN \"LOADER\"\r" >new/\!BOOT

# Create INF file for LOADER
myfilesize=$(stat -f %z "new/LOADER")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.LOADER     FF1900 FF1900 $myfilesizehex" >new/\LOADER.INF

# Create INF file for !BOOT
myfilesize=$(stat -f %z "new/!BOOT")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.!BOOT     FF1900 FF1900 $myfilesizehex" >new/\!BOOT.INF

# Create INF file for JSW1
myfilesize=$(stat -f %z "new/JSW1")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.JSW1 00001100 00001F90 $myfilesizehex L" >new/JSW1.INF

# Create INF file for JSW1.5
myfilesize=$(stat -f %z "new/JSW1.5")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.JSW1.5 00004700 00004D00 $myfilesizehex L" >new/JSW1.5.INF


# grep jsw1.sym -e 'allFree' | sed 's/^.*\([0-9a-f][0-9a-f][0-9a-f][0-9a-f]\).*/\1/' | awk '{printf "%d bytes free\n",(("0x" $1) + 0)}'

echo
# Create new SSD file with the appropriate files
../tools/bbcim -new -type ADFS JSW.ssd
../tools/bbcim -boot JSW.ssd EXEC
../tools/bbcim -a JSW.ssd new/!BOOT new/loader new/jsw1 new/jsw1.5

echo

if [ $run == "1" ];
then
    if [ $USER == "tobynelson" ];
    then
        # Open SSD in b2
        DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

        open -a 'b2 Debug'
        sleep 1
        # curl -G 'http://localhost:48075/reset/b2' --data-urlencode "config=Master 128 (MOS 3.50)"
        curl -H 'Content-Type:application/binary' --upload-file "$DIR/JSW.ssd" 'http://localhost:48075/run/b2?name=JSW.ssd'

    else
        # Open SSD in BeebEm
        open JSW.ssd
    fi
fi
