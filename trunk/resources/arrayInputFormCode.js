            var butAdds = frm.find('span.button.add');   
            for(var x=0;x<butAdds.length;x++){   
               $(butAdds[x]).bind('click',{button:$(butAdds[x])},function(event){   
                  var inp = $(event.data.button.prev()).clone();   
                  inp.val('');   
                  event.data.button.before('<br/>');   
                  event.data.button.before(inp);   
               });   
            }   