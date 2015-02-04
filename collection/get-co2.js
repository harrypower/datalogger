var co2 = require('/usr/local/lib/node_modules/bonescript');

co2.analogRead('P9_40', co2print);

function co2print(x) {
    console.log('co2: ' + x.value);
    console.log('co2 reading error: ' + x.err);
}
