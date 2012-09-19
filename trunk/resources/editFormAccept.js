      var butAccept = $(frm.find('span.accept:last')[0]);
      butAccept.unbind('click');
      butAccept.bind('click', { view: view, frm: frm }, function(event) {
          var frm = event.data.frm;
          var model = event.data.view.model;
          var changes = {};
          var inputs = frm.find('input,select');
          for (var x = 0; x < inputs.length; x++) {
              var name = $(inputs[x]).attr('name');
              var inp = $(inputs[x]);
              if (inp.attr('modeltype') != '') {
                  if (inp.attr('multiple') == 'multiple') {
                      changes[inp.attr('name')] = new Array();
                      var opts = inp.find('option:selected');
                      for (var y = 0; y < opts.length; y++) {
                          changes[inp.attr('name')].push(eval('new ' + inp.attr('modeltype') + '({id:\'' + $(opts[y]).val() + '\'});'));
                      }
                  } else {
                      if (model.get(inp.attr('name')).get('id') != inp.val()) {
                          changes[inp.attr('name')] = eval('new ' + inp.attr('modeltype') + '({id:\'' + inp.val() + '\'});');
                      }
                  }
              } else if (inp.attr('multiple') == 'multiple') {
                  changes[inp.attr('name')] = new Array();
                  var opts = inp.find('option:selected');
                  for (var y = 0; y < opts.length; y++) {
                      changes[inp.attr('name')].push($(opts[y]).val());
                  }
              } else if (inp.attr('isarray') == 'true') {
                  if (changes[inp.attr('name')] == undefined) {
                      changes[inp.attr('name')] = new Array();
                      var ainps = frm.find('input[name="' + inps.attr('name') + '"]');
                      for (var y = 0; y < ainps.length; y++) {
                          changes[inp.attr('name')].push($(ainps[y]).val());
                      }
                  }
              } else {
                  if (model.get(inp.attr('name')) != inp.val()) {
                      changes[inp.attr('name')] = inp.val();
                  }
              }
          }
         if (!_.isEmpty(changes)){
            if (!model.set(changes)){
               Backbone.ShowModelError(model);
            }else{
               model.save();
            }
         }else{
            view.render();
         }
      });  