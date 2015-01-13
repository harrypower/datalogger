require objects.fs

object class
    cell% inst-var string-addr
    cell% inst-var string-size
    cell% inst-var valid
    m: ( string -- ) \ initalize the string
	valid @ valid =
	if string-addr @ free throw then
	0 valid !
	0 string-addr !
	0 string-size ! ;m overrides construct
    m: ( caddr u string -- ) \ store string
	valid @ valid =
	if
	    string-addr @ free throw
	    0 valid ! 
	then
	dup 0 >
	if
	    dup allocate throw
	    dup string-addr !
	    swap dup string-size ! move
	    valid valid !
	else 2drop
	then ;m method !$
    m: ( string -- caddr u ) \ retrieve string
	valid @ valid =
	if
	    string-addr @ string-size @
	else 0 0
	then ;m method @$
    m: ( caddr u string -- ) \ add a string to this string
	valid @ valid =
	if \ resize
	    dup 0 >
	    if
		dup string-size @ + string-addr swap resize throw
		dup string-addr ! string-size @ + swap dup string-size @ + string-size @
		move
	    else 2drop
	    then
	else
	    dup 0 >
	    if
		dup allocate throw
		dup string-addr !
		swap dup string-size ! move
		valid valid !
	    else 2drop
	    then
	then ;m method !+$
    m: ( string -- ) \ retrieve string object info
	this [parent] print
	s"  valid:" type valid @ valid = .
	s"  addr:" type string-addr @ .
	s"  size:" type string-size @ .
	s"  string:" type string-addr @ string-size @ type ;m overrides print	
    end-class string