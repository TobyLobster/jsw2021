set -e

./tools/bin/EncodeData --inputfile definitions.txt --outputfile definitions.a

mkdir -p new
../tools/acme -o new/jsw1 -r jsw1.txt --vicelabels jsw1.sym jsw1.a

freeSpace=$(cat jsw1.sym | grep -e '\.free_total' | sed 's/.*C:\([0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]\).*/\1/')

echo "Free space: $((16#$freeSpace))"

sort -o jsw1.tmp jsw1.sym
uniq jsw1.tmp jsw1.sym
rm jsw1.tmp

run="1"
skipdiff="0"
for var in "$@"
do
    if [ "$var" == "norun" ]; then
        run="0"
    fi
done

printf "*BASIC\rPAGE=&1900\r*FX21\rCLOSE#0:*RUN \"JSW1\"\r" >new/\!BOOT

# Show entry point
entry_point=$(grep jsw1.sym -e '\.entry_point$' | sed 's/.*C:\([0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]\).*/\1/')

# Create INF file for !BOOT
myfilesize=$(stat -f %z "new/!BOOT")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.!BOOT     FF1900 FF1900 $myfilesizehex" >new/\!BOOT.INF

# Create INF file for JSW1
myfilesize=$(stat -f %z "new/JSW1")
myfilesizehex=$(printf '%x\n' $myfilesize)
echo "$.JSW1 00001100 0000$entry_point $myfilesizehex L" >new/JSW1.INF

echo
# Create new SSD file with the appropriate files
../tools/bbcim -new -type ADFS JSW.ssd
../tools/bbcim -boot JSW.ssd EXEC
../tools/bbcim -a JSW.ssd new/!BOOT new/jsw1

echo

if [ $run == "1" ];
then
    if [ $USER == "tobynelson" ];
    then
        # Open SSD in b2
        DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

        open -a 'b2 Debug'

        # wait for the application to start up and respond to our requests
        set +e
        until curl -G 'http://localhost:48075/peek/WIN/0/0' >/dev/null 2>&1
        do
            sleep 0.1
        done
        set -e

        # curl -G 'http://localhost:48075/reset/b2' --data-urlencode "config=Master 128 (MOS 3.50)"
        curl -H 'Content-Type:application/binary' --upload-file "$DIR/JSW.ssd" 'http://localhost:48075/run/b2?name=JSW.ssd'

    else
        # Open SSD in BeebEm
        open JSW.ssd
    fi
fi
