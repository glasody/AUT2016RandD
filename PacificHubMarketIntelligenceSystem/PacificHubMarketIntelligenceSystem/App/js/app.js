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
    '$scope', '$http', '$cookies', function($scope, $http, $cookies) {
        $scope.newSub = {};
        $scope.feedList = {};
        $scope.newSub.user = "default";
        $scope.graphData = {
            "nodes": [
              {
                  "id": 1
              },
              {
                  "id": 2
              },
              {
                  "id": 3
              }
            ],
            "edges": [
              {
                  "source": 1,
                  "target": 2
              },
              {
                  "source": 1,
                  "target": 3,
              }
            ]
        };
        $scope.keywords = {};

        alchemy.begin({ "dataSource": $scope.graphData });
        
		$scope.getSubscriptions = function (currentUser) {
		    console.log("Getting Subscriptions...");
		    $http({
		        method: 'GET',
		        url: '/api/rss/subscriptions',
		        params: {
		            user: currentUser
		        }
		    })
		    .then(function successCallback(response) {
		        console.log("Subscriptions Load Complete!");
		        $scope.subscriptionList = response.data;
		    }, function errorCallback(response) {
		        console.log(response);
		        });
		}

        $scope.subscribe = function (newSub) {
            console.log("Subscribing...");
            $http({
                method: 'POST',
                url: '/api/rss/subscribe',
                data: newSub
            }).then(function successCallback(response) {
                console.log("Subscribed");
                newSub.title = "";
                newSub.uri = "";
                $scope.getSubscriptions($scope.newSub.user);
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
                console.log("Fetched");
                $scope.feedList = response.data;
                $scope.getKeywords();
            }, function errorCallback(response) {
                console.log(response);
            });
        }

        $scope.download = function(data, name, type) {
            var a = document.createElement("a");
            var file = new Blob([data], { type: type });
            a.href = URL.createObjectURL(file);
            a.download = name;
            a.click();
        }

        $scope.deleteSubscription = function (uri) {
            $http({
                    method: 'Post',
                    url: '/api/rss/deleteSubscription',
                    params: {
                        uri: uri
                    }
                })
                .then(function successCallback(response) {
                    console.log("Removed");
                    $scope.getSubscriptions($scope.newSub.user);
                    },
                    function errorCallback(response) {
                        console.log(response);
                    });
        }

        $scope.getArticle = function(url) {
            window.open(url);
        }

        $scope.getKeywords = function() {
            $http({
                    method: 'Get',
                    url: '/api/newsfeed/keywords'
                })
                .then(function successCallback(response) {
                    $scope.keywords = response.data;
                }, function errorCallback(response) {
                    console.log(response);
                });
        }

        $scope.query = function (word) {
            $http({
                method: 'Get',
                url: '/api/graph/queryKeyword',
                params: {
                    keyword: word.value
                }
            }).then(function successCallback(response) {
                console.log(response.data);
                $scope.graphData = response.data;
            }, function errorCallback(response) {
                console.log(response);
            });
        }

        $scope.clearDB = function () {
            console.log("Clearing Database...");
            $http({
                method: 'Post',
                url: '/api/graph/clearDatabase'
            }).then(function successCallback(response) {
                console.log("Database Cleared!");
                console.log(response.data);
            }, function errorCallback(response) {
                console.log(response);
            });
        }
    }
]);