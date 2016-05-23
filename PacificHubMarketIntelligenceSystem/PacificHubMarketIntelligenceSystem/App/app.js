$(document).foundation();
var AngularApp = angular.module('app',
[
    'ngRoute',
    'ngCookies'
]);

AngularApp.config(['$routeProvider', function($routeProvider) {
    $routeProvider
        .when('/',
        {
            templateUrl: 'index.html',
            controller: 'AngularParentController'
        })
        .otherwise({
            redirectTo: '/'
        });
}]);

AngularApp.controller('AngularParentController',
[
    '$scope', '$http', function($scope, $http) {

    }
]);