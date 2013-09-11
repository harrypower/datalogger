#include "rpiGpio.h"
#include <stdio.h>
#include <unistd.h>
#include <stdlib.h>
#include <time.h>
#include <sys/types.h>

int main(int argc,char *argv[])
{

  int loop = 0;
  int errn;
  eState value = 0 ;
  eState value2 = 0 ;

  errn = gpioSetup();
  //  printf("Setup errn %d\n", errn);

  errn = gpioSetFunction(7,input);
  // printf("Set function errn %d\n", errn);

  errn = gpioReadPin(7,&value);
  // printf("Read errn %d\n", errn);
  // printf("Value before is %d\n", value);

  errn = gpioSetPullResistor(7,pullDisable);
  // printf("Pull errn %d\n", errn);

  errn = gpioSetFunction(7,output);
  // printf("function errn %d\n", errn);
 
  errn = gpioSetPin(7,0);
  // printf("Pin %d\n", errn);

  usleep(2000);

  gpioSetFunction(7,input);

  gpioReadPin(7,&value);
  
  do 
    {
      loop++;
      gpioReadPin(7,&value2);
    }
  while ( value2 < 1 );

  errn = gpioCleanup(); 
 
  // printf("cleanup %d\n", errn);
  // printf("Value is %d\n",value);
  // printf("Value2 is %d\n",value2);
  printf("loop: %d",loop);
  //  printf("my pid is %jd\n", (intmax_t) getpid());
  //  printf("Parent pid is %jd\n", (intmax_t) getppid());

  return (0);
}
