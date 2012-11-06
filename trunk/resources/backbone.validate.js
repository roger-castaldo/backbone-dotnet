Backbone.ErrorMessages = {};
Backbone.Language = 'en';
Backbone.TranslateValidationError = function(errorMessage) {
    var props = ((Backbone.Language)+'.' + errorMessage).split('.');
    var obj = Backbone.ErrorMessages;
    var ret = errorMessage;
    for (var x = 0; x < props.length; x++) {
        if (obj[props[x]] != undefined) {
            obj = obj[props[x]];
        } else {
            obj = null;
            x = props.length;
        }
    }
    if (obj != null) {
        ret = obj;
    }
    return ret;
};
Backbone.ShowModelError = function(model) {
    var erDialog = $('#Backbone_Error_Dialog');
    if (erDialog.length == 0) {
        erDialog = $('<div id="Backbone_Error_Dialog" class="dialog error"></div>');
        $(document.body).append(erDialog);
    }
    erDialog.html('');
    var ul = $('<ul></ul>');
    erDialog.append(ul);
    for (var x = 0; x < model.errors.length; x++) {
        ul.append('<li class="' + model.errors[x].field + '"><span class="error_field_name">' + model.errors[x].field + '</span><span class="error_field_message">' + model.errors[x].error + '</span></li>');
    }
    erDialog.show();
}
Backbone.Model.prototype.syncSave = function(attrs, options) {
    if (!options) { options = {}; }
    options = _.extend(options, { async: false });
    this.save(attrs, options);
}
Backbone.Model.prototype.syncDestroy = function(attrs, options) {
    if (!options) { options = {}; }
    options = _.extend(options, { async: false });
    this.destroy(attrs, options);
}