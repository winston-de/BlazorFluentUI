
window.BlazorFabricMaskTextField = {

    getSelectionStart: function (element) {
        return element.selectionStart;
    },

    getSelectionEnd: function (element) {
        return element.selectionEnd;
    },

    setSelectionRange : function(element, start, end) {
        element.setSelectionRange(start, end);
    }
}