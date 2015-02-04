var nh3 = require('/usr/local/lib/node_modules/bonescript');

nh3.analogRead('P9_39', nh3print);

function nh3print(x) {
    console.log('nh3: ' + x.value);
    console.log('nh3 reading error: ' + x.err);
}