original=$1
output=$2

for i in $1/*.lz; do
	file=`basename "${i%.*}"`
	mono AttackFridayMonsters/AttackFridayMonsters/bin/Debug/AttackFridayMonsters.exe -e script $i $2/$file.po
done
