#! /usr/bin/gforth

clear-libs
c-library mysemaphore
s" rt" add-lib
\c #include <semaphore.h> 
\c #include <sys/stat.h>
\c #include <fcntl.h>


\c int info(sem_t * fail){
\c    fail = SEM_FAILED ;
\c    return( O_CREAT | O_EXCL ) ; }

c-function sema-info info a -- n
c-function sem-open sem_open a n n n -- a
c-function sem-openexisting sem_open a n -- a 
c-function sem-close sem_close a -- n
c-function sem-unlink sem_unlink a -- n
c-function sem-getvalue sem_getvalue a a -- n
c-function sem-dec sem_wait a -- n
c-function sem-inc sem_post a -- n
c-function sem-trydec sem_trywait a -- n

end-c-library

variable sem_t*
variable sem_failed
variable semaphore-value
436 constant root-user-access  \ this is the same as in c 0664 or octal 664 ( root read/write, user read/write, world read )

: *char ( caddr u -- caddr ) dup 2 + pad swap erase pad swap move pad  ;

: named-sema-open { caddr u nvalue -- nflag } \ nflag is false for named sema created true for named sema failed to create might even already exist!
    caddr u *char             \ (char * name) name string of semaphore ready to pass to c
    sem_failed sema-info      \ (int oflag) O_CREAT | O_EXCL value from system ready to pass to c
    root-user-access          \ (mode_t mode) this is like 0664 and ready to pass to c
    nvalue semaphore-value !  \ stored the start value to transfer into semaphore 
    nvalue                    \ now that value is ready to pass to c
    sem-open                  \ open the new semaphore and see if it works or not
    sem_t* !                  \ stored the sem_t * pointer to use later
    sem_t* @ sem_failed @  =  \ compare with failure condition
;                             \ Note sem_t* now contains the address to the semaphore to use to access this named semaphore

: semaphore-value ( -- nvalue nflag )  \ nflag will false if the nvalue is valid
    sem_t* @ semaphore-value sem-getvalue semaphore-value @ swap ;

: existing-sema-open ( caddr u -- nflag )  \ nflag is false if existing named semaphore is opened for access
    *char                           \ (char * name) name string of semaphore ready to pass to c
    sem_failed sema-info drop       \ getting semaphore failed value 
    0 sem-openexisting              \ 0 tells sem_open() function to open existing named semaphore
    sem_t* !                        \ store sem_t* pointer to use later
    sem_t* @ sem_failed @ =         \ compare with failure condition
;                                   \ Note now sem_t* contains the address to the semaphore for accessing this named semaphore

: open-existing-sema ( caddr u -- asem_t* nflag ) \ nflag is false if existing named semaphore is opened for access asem_t* is pointer to semaphore
    *char                   \ (char * name) name string of semaphore ready to pass to c
    0 sem-openexisting dup  \ 0 tels sem_open() function to open existing named semaphore
    pad sema-info drop      \ compare with failure condition
    pad @ =  ;              \ return (sem_t *) pointer to semaphore and flage  

: semaphore@ ( asem_t* -- nvalue nflag )
    pad sem-getvalue pad @ swap ;

: open-named-sema ( caddr u nvalue -- asem_t* nflag )
    >r *char
    pad sema-info
    436
    r>
    sem-open dup
    pad @ = ;

: close-semaphore ( asem_t* -- nflag )
    sem-close ;

: remove-semaphore ( caddr u -- nflag )
    *char sem-unlink ;

: semaphore+ ( asem_t* -- nflag )
    sem-inc ;

: semaphore- ( asem_t* -- nflag )
    sem-dec ;

: semaphore-try- ( asem_t* -- nflag )
    sem-trydec ;