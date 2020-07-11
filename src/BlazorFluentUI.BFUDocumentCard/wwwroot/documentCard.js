/// <reference path="../../BlazorFluentUI.BFUBaseComponent/wwwroot/baseComponent.ts" />
var BlazorFluentUiDocumentCard;
(function (BlazorFluentUiDocumentCard) {
    var CardTitleMap = /** @class */ (function () {
        function CardTitleMap() {
        }
        CardTitleMap.prototype.stateChanged = function () {
            this.dotnet.invokeMethodAsync("UpdateTitle", this.state.truncatedTitleFirstPiece, this.state.truncatedTitleSecondPiece);
        };
        return CardTitleMap;
    }());
    var CardTitleState = /** @class */ (function () {
        function CardTitleState(shouldTruncate) {
            this.needMeasurement = !!shouldTruncate;
        }
        return CardTitleState;
    }());
    var cardTitles = new Array();
    function getElement(id) {
        for (var i = 0; i < cardTitles.length; i++) {
            if (cardTitles[i].id === id) {
                return cardTitles[i];
            }
        }
        return null;
    }
    BlazorFluentUiDocumentCard.getElement = getElement;
    function addElement(id, element, dotnet, shouldTruncate, orgTitle) {
        var title = new CardTitleMap();
        title.state = new CardTitleState(shouldTruncate);
        title.state.originalTitle = orgTitle;
        title.state.previousTitle = orgTitle;
        title.id = id;
        title.element = element;
        title.dotnet = dotnet;
        title.state.watchResize = shouldTruncate;
        title.resizeFunction = function (e) {
            if (!title.state.watchResize) {
                return;
            }
            title.state.watchResize = false;
            setTimeout(function () {
                console.log('resize');
                title.dotnet.invokeMethodAsync("UpdateneedMeasurement");
                title.state.truncatedTitleFirstPiece = '';
                title.state.truncatedTitleSecondPiece = '';
                truncateTitle(title);
                title.state.watchResize = true;
            }, 500);
        };
        window.addEventListener('resize', title.resizeFunction);
        cardTitles.push(title);
    }
    BlazorFluentUiDocumentCard.addElement = addElement;
    function removelement(id) {
        var index = -1;
        for (var i = 0; i < cardTitles.length; i++) {
            if (cardTitles[i].id === id) {
                index = i;
                break;
            }
        }
        if (index >= 0) {
            var title = cardTitles[index];
            window.removeEventListener('resize', title.resizeFunction);
            cardTitles.splice(index, 1);
        }
    }
    BlazorFluentUiDocumentCard.removelement = removelement;
    function initInternal(title) {
        if (title.state.needMeasurement) {
            requestAnimationFrame(function (time) {
                truncateTitle(title);
            });
        }
    }
    function initTitle(id, element, dotnet, shouldTruncate, orgTitle) {
        var title = getElement(id);
        if (title === null) {
            addElement(id, element, dotnet, shouldTruncate, orgTitle);
            title = getElement(id);
            initInternal(title);
        }
    }
    BlazorFluentUiDocumentCard.initTitle = initTitle;
    function truncateTitle(cardTitle) {
        if (!cardTitle) {
            return;
        }
        var TRUNCATION_VERTICAL_OVERFLOW_THRESHOLD = 5;
        var el = document.getElementById(cardTitle.id);
        var style = getComputedStyle(el);
        if (style.width && style.lineHeight && style.height) {
            var clientWidth = el.clientWidth, scrollWidth = el.scrollWidth;
            var lines = Math.floor((parseInt(style.height, 10) + TRUNCATION_VERTICAL_OVERFLOW_THRESHOLD) / parseInt(style.lineHeight, 10));
            var overFlowRate = scrollWidth / (parseInt(style.width, 10) * lines);
            if (overFlowRate > 1) {
                var truncatedLength = cardTitle.state.originalTitle.length / overFlowRate - 3 /** Saved for separator */;
                cardTitle.state.truncatedTitleFirstPiece = cardTitle.state.originalTitle.slice(0, truncatedLength / 2);
                cardTitle.state.truncatedTitleSecondPiece = cardTitle.state.originalTitle.slice(cardTitle.state.originalTitle.length - truncatedLength / 2);
                cardTitle.stateChanged();
            }
        }
    }
})(BlazorFluentUiDocumentCard || (BlazorFluentUiDocumentCard = {}));
//# sourceMappingURL=documentCard.js.map