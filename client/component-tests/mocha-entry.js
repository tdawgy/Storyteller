require('./phantomjs-polyfill');
require('./regexp-polyfill');
//provide TestUtils as a global
global.TestUtils = require('react-dom/test-utils');



require('./../components/editing/component-loader');

//require all modules beginning with 'test-' from the current
//directory and its immediate subdirectories
var testsContext = require.context('.', true, /^.\/test-/);

testsContext.keys().forEach(testsContext)


