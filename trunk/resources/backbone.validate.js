Backbone = _.extend(Backbone,{
    ErrorMessages : {},
    Language : 'en',
    TranslateValidationError : function(errorMessage) {
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
    },
    ShowModelError : function(model) {
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
    },
    Model:_.extend(Backbone.Model,{
        prototype: _.extend(Backbone.Model.prototype,{
            syncSave:function(attrs, options) {
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.save(attrs, options);
            },
            syncDestroy:function(attrs,options){
                if (!options) { options = {}; }
                options = _.extend(options, { async: false });
                return this.destroy(attrs, options);
            },
            change: function(options) {
                options || (options = {});
                var changing = this._changing;
                this._changing = true;

                // Silent changes become pending changes.
                for (var attr in this._silent) this._pending[attr] = true;

                // Silent changes are triggered.
                var changes = _.extend({}, options.changes, this._silent);
                this._silent = {};
                for (var attr in changes) {
                    this.trigger('change:' + attr, this, this.attributes[attr], options);
                }
                if (changing) return this;

                // Continue firing `"change"` events while there are pending changes.
                while (!_.isEmpty(this._pending)) {
                    this._pending = {};
                    this.trigger('change', this, options);
                    // Pending and silent changes still remain.
                    for (var attr in this.changed) {
                        if (this._pending[attr] || this._silent[attr]) continue;
                        delete this.changed[attr];
                    }
                    this._previousAttributes = _.clone(this.attributes);
                }

                this._changing = false;
                return this;
            }
        })
    })
});