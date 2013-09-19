#! /usr/bin/gforth

clear-libs
c-library mysemaphore
s" rt" add-lib
\c #include <semaphore.h> 
\c #include <sys/stat.h>
\c #include <fcntl.h>

\c sem_t * sem ;
\c int readvalue = 0 ;
\c unsigned int startvalue = 0 ;
\c const char * name = "mysema" ;
\c char * name2 ;

\c int semanameopen(char * names, int thevalue){
\c   if ((sem = sem_open(names, O_CREAT | O_EXCL , 0664, thevalue)) == SEM_FAILED)
\c     { return(1); }
\c     else
\c     { return(0); }
\c   }
\c int semadoopen(){ 
\c   if ((sem = sem_open(name, O_CREAT | O_EXCL , 0664, startvalue )) == SEM_FAILED)
\c     { return(1); }
\c     else 
\c     { return(0); }
\c   }
\c int semaopenexisting(){
\c   if ((sem = sem_open(name, 0)) == SEM_FAILED)
\c     { return (1) ; }
\c     else
\c     { return (0) ; }
\c   }
\c int semadoclose(){ return ( sem_close( sem )) ; }
\c int semadogetvalue(){ return ( sem_getvalue( sem, &readvalue )) ; }
\c int semadounlink(){ return ( sem_unlink( name )) ; }
\c int semawait(){ return ( sem_wait( sem )); }
\c int semapost(){ return ( sem_post( sem )); }
\c int semavalue(){ return ( readvalue ) ; }
\c int sematrywait(){ return ( sem_trywait( sem )) ; }
\c int semasetvalue( int value ){
\c    startvalue = (unsigned int)value ;
\c    return(0) ; 
\c    } 

c-function sema-nameopen semanameopen a n -- n 
c-function sema-doopen semadoopen -- n
c-function sema-openexisting semaopenexisting -- n
c-function sema-doclose semadoclose -- n
c-function sema-dogetvalue semadogetvalue -- n
c-function sema-dounlink semadounlink -- n
c-function sema-dec semawait -- n
c-function sema-inc semapost -- n
c-function sema-value semavalue -- n
c-function sema-trydec sematrywait -- n
c-function sema-setvalue semasetvalue n -- n

end-c-library

