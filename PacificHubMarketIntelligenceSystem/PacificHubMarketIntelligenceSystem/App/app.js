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
    '$scope', '$http', function ($scope, $http) {
        $scope.newSub = {};
        $scope.subscriptionsList = {};
        $scope.newSub.user = "default";

        $scope.subscribe = function (newSub) {
            console.log("Subscribing...");
            $http({
                method: 'POST',
                url: '/api/rss/subscribe',
                data: newSub
            }).then(function successCallback(response) {
                console.log(response.data);
            }, function errorCallback(response) {
                console.log(response);
            });
        }

        $scope.fetch = function() {
            console.log("Fetching...");
            $http({
                method: 'Get',
                url: '/api/rss/fetch',
                params: {
                    user : $scope.newSub.user
                }
            }).then(function successCallback(response) {
                $scope.subscriptionsList = response.data;
                $scope.download(JSON.stringify($scope.subscriptionsList, null, "\t"), "output.txt", "application/json");
                $scope.download(JSON.stringify($scope.subscriptionsList, null, "\t"), "output.json", "application/json");
            }, function errorCallback(response) {

            });
        }

        $scope.download = function(data, name, type) {
            var a = document.createElement("a");
            var file = new Blob([data], { type: type });
            a.href = URL.createObjectURL(file);
            a.download = name;
            a.click();
        }
    }
]);