\ This Gforth code is a sqlite3 wrapper library 
\    Copyright (C) 2013  Philip King. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.

\ Note the following is needed on your Debian (tested with wheezy):
\ sudo apt-get install sqlite3 libsqlite3-dev
\ These apt-get installs are needed to connect this code to sqlite3 code

\ I will put here how to use this code when i get there!


clear-libs
c-library mysqlite3

s" sqlite3" add-lib

\c #include <stdio.h>
\c #include "sqlite3.h"
\c #include <string.h>
\c
\c char * theBuffer = 0;
\c int maxBuffsize = 0;
\c char * separator = 0;
\c
\c static int callback( void * NotUsed, int argc, char ** argv, char ** azColName ) {
\c int i;
\c char temp[maxBuffsize/4] ;
\c int tempSize;
\c int tempBuffsize;
\c
\c for(i = 0; i < argc ; i++) {
\c      sprintf(temp,"%s",argv[i] ? argv[i] : "NULL");
\c      strcat(temp,separator);    
\c      tempSize = strlen(temp);
\c      tempBuffsize = strlen(theBuffer);
\c      if((tempBuffsize + tempSize + 1) < maxBuffsize){
\c           strcat(theBuffer,temp);
\c           } else { return 0; }
\c      }
\ This was from testing but i left it here to remember the variable meanings!
\ \c for(i=0; i < argc ; i++) {
\ \c    printf( "%s = %s\n", azColName[i], argv[i] ? argv[i] : "NULL" );
\ \c    }
\c return 0;  
\c }
\c
\c int sqlite3to4th( const char * filename, char * sqlite3_cmds, char * sqlite3_ermsg , char * buffer, int buffsize , char * sep) {
\c sqlite3 *db;
\c char * zErrMsg = 0 ;
\c int rc = 0 ;
\c theBuffer = buffer;
\c maxBuffsize = buffsize;
\c separator = sep;
\c
\c rc = sqlite3_open( filename, &db ) ;
\c if( rc ) {
\c    sprintf( sqlite3_ermsg,"Can't open database: %s\n", sqlite3_errmsg( db ) );
\c    sqlite3_close( db );
\c    return 1;
\c }
\c    
\c rc = sqlite3_exec( db, sqlite3_cmds, callback, 0, &zErrMsg );
\c if( rc!=SQLITE_OK ) {
\c   sprintf( sqlite3_ermsg, "SQL error: %s\n", zErrMsg );
\c   sqlite3_free( zErrMsg );
\c }
\c sqlite3_close( db );
\c return 0;
\c }
\c

\ **** sqlite3 gforth wrappers ****

c-function sqlite3 sqlite3to4th a a a a n a -- n
\ note that c strings are always null terminated unlike gforth strings! 
    
end-c-library

: mkZstr ( caddr u -- caddr1 nflag )  \ make a z string to pass to c code that can be freed later
    \ nflag is 0 if caddr1 is a valid memory location with caddr u transfered to it and can be freed later
    TRY
	0 { caddr u caddr1 }
	u 1+ allocate throw to caddr1
	caddr1 u 1+ erase
	caddr caddr1 u move caddr1 false
    RESTORE dup if swap drop then 
    ENDTRY ;
