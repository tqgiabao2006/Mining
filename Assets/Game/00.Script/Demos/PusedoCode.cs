/*

A* PATH FINDING

PSEUDO CODE;
OPEN //the set of nodes to be evaluated
CLOSED //the set of nodes already evaluated add the start node to OPEN
loop
current = node in OPEN with the lowest f
_cost
remove current from OPEN
add current to CLOSED
if current is the target node //path has been found return
foreach neighbour of the current node
if neighbour is not traversable or neighbour is in CLOSED
skip to the next neighbour
if new path to neighbour is shorter OR neighbour is not in OPEN
set f
_cost of neighbour
set parent of neighbour to current if neighbour is not in OPEN add neighbour to OPEN



*/