/// <reference path="../../Helper.js" />



var Comment = {

    MakeReadOnly:false,

	// entry point for this javascript object
    Initialize: function (id, permitStatus, refTableId, userName) {

        Comment.PermitId = id;
        Comment.PermitStatus = permitStatus;
        Comment.RefTableId = refTableId;
        Comment.UserName = userName;
        Comment.SetGrid(Comment.PermitId);
        Comment.addCommentButtonClick();
        
    },

    SetPermitStatus: function (permitStatus) {
        Comment.PermitStatus = permitStatus;
    },

    addCommentButtonClick: function () {
        $("#addCommentButton").click(function () {
            if (Comment.CommentEdit) {
                Comment.CommentEdit.Id = 0;
            }
            $('[name=addCommentForm]').detach()
            $("#saveCommentButton").detach();
            $("#dialog").html("")
            $('#dialog').prepend("<form enctype='multipart/form-data' method='post' name='addCommentForm' id='form1'><textarea id='theComment' rows=6 style='resize:none;max-height:200px;width:400px;'></textarea><br></span><br></form><div style='text-align:center'><input id='saveCommentButton' type=button value='Add' /><input id='cancelButton' type='button' value='Cancel' /></div>")
            Comment.saveCommentButtonClick();
            Comment.cancelButton();
            $("#dialog").dialog({
                modal: true,
                position: "center",
                width:440,
                resizable: false,
                //jqueryUI buttons
                open: function (event, ui) {
                    //style dialog buttons
                    styleDialogInputs();
                }
            });
        });
    },

    cancelButton: function () {
        $("#cancelButton").click(function () {
            $("#dialog").dialog("close");
            $("#dialog").dialog("destroy");
        });
    },

    saveCommentButtonClick: function () {
        
        $("#saveCommentButton").click(function () {
            $.ajax({
                url: Helper.GetBaseURL("Permitting") + "Comment/SaveComment",
                data: { Comment1: $('#theComment').val(), PermitStatusId: Comment.PermitStatus, RefId: Comment.PermitId, RefTableId: Comment.RefTableId, Id: Comment.CommentEdit === undefined ? 0 : Comment.CommentEdit.Id },
                success: function (response) {
                    $("#dialog").dialog("close");
                    $("#dialog").dialog("destroy");
                    $('#commentDashboardGrid').trigger("reloadGrid")
                }
            });
        }
        );
    },

    makeReadOnly: function () {
        $('.commentsTab').attr('disabled', 'disabled')
        $('.commentsTab').addClass('disabled ui-button-disabled ui-state-disabled')

        Comment.MakeReadOnly = true;
    },

    disableAllDeletingExceptForNoStatus: function () {
        $('.hasStatus').attr('onclick', '').unbind('click').addClass('disabled ui-button-disabled ui-state-disabled');
        Comment.DisableAllDeletingExceptForNoStatus = true;
    },

    SetGrid: function (id) {
        
        $("#commentDashboardGrid").jqGrid({
            colModel: [
                { name: 'Comment1', width: 350 },
                { name: 'LastModifiedDate', formatter: Comment.DateFormatter },
                { name: 'LastModifiedBy' },
                { name: 'permitStatusId',  jsonmap: "PermitStatusId" },
                { name: 'view' },
                { name: 'delete' }
            ],
            postData: { permitStatus: Comment.PermitStatus },
            colNames: ['Comment','Comment Date','Comment By','Permit Status','',''],
            url: Helper.GetBaseURL("Permitting") + "Comment/Data?id=" + id,
            datatype: "json",
            gridComplete: function () {
                $("#commentDashboardGrid a").click(function (e) {
                    e.preventDefault()
                }
                )


                if (jQuery("#commentDashboardGrid").jqGrid('getGridParam', 'records') == 0) {
                    $("#gbox_commentDashboardGrid").hide()
                    $("#gbox_commentDashboardGrid #pager").hide()
                    $("#MsgNoComment").detach()
                    $("#addCommentButton").parent().append('<p id="MsgNoComment">No Comments Have Been Added</p>')
                } else {
                    $("#MsgNoComment").detach()
                    $("#gbox_commentDashboardGrid").show()
                    $("#gbox_commentDashboardGrid #pager").show()
                    $("#commentDashboardGrid").jqGrid("fixGridWidth")
                }

                if (Comment.DisableAllDeletingExceptForNoStatus) {
                    $('.hasStatus').attr('onclick', '').unbind('click').addClass('disabled ui-button-disabled ui-state-disabled');
                }
            
            },
 
            afterInsertRow: function (rowid, aData, rowelem) {
                if (rowelem.CreatedBy == Comment.UserName && !Comment.MakeReadOnly) {
                    del = "<a href='#' class='" + (rowelem.StatusId == -1 ? 'noStatus' : 'hasStatus') + "' onclick=\"Comment.DelRow(" + rowelem.Id + "," + rowid + ");\">Delete</a>"
                } else {
                    del = "<span class='disabled ui-button-disabled ui-state-disabled'>Delete</span>";
                }
                view = "<a href='#' onclick=\"Comment.ViewRow(" + rowelem.Id + ");\">View</a>"
                jQuery("#commentDashboardGrid").jqGrid('setRowData', rowid, { 'delete': del });
                jQuery("#commentDashboardGrid").jqGrid('setRowData', rowid, { 'view': view });
                
            },
            height:"auto"
        });

    },

    DateFormatter: function (cellvalue, options, rowObject) {
        return (new Date(parseInt(cellvalue.substr(6, 13)))).format("m/dd/yyyy");
    },

    DelRow: function (commentId, permitId) {

        $("#dialog2").dialog({
            modal: true,
            position: "center",
            width: 330,
            resizable: false,
            //jqueryUI buttons
            open: function (event, ui) {
                //style dialog buttons
                styleDialogInputs();
            },
            buttons: {
                "Yes": function () {
                    $.ajax({
                        url: Helper.GetBaseURL("Permitting") + "Comment/Delete",
                        data: { id: commentId },
                        success: function (response) {
                            $("#commentDashboardGrid").trigger("reloadGrid");
                        }
                    });

                    $("#dialog2").dialog("close");
                    $("#dialog2").dialog("destroy");
                },
                "No": function () {
                    $("#dialog2").dialog("close");
                },
            }
        });
    },

    ViewRow: function (commentId) {
        $.ajax({
            url: Helper.GetBaseURL("Permitting") + "Comment/GetComment",
            data: { id: commentId },
            success: function (response) {
                Comment.CommentEdit = response.comment;
                Comment.saveCommentButtonClick();
                
                $('[name=saveCommentForm]').detach()
                $("#saveCommentButton").detach();
                $("#dialog").html("")
                $('#dialog').prepend("<form enctype='multipart/form-data' method='post' name='saveCommentForm' id='form1'><textarea id='theComment'  rows=6 style='resize:none;max-height:200px;width:400px;'>" + response.comment.Comment1 + "</textarea><br></span><br></form><div style='text-align:center'><input id='saveCommentButton' type=button value='Save' /><input id='cancelButton' type='button' value='Cancel' /></div>")
                Comment.saveCommentButtonClick();
                Comment.cancelButton();

                //if the current logged in user is not the person who created the comment
                // of if comment grid is set to be read only, disable the save button
                if(response.userName != response.comment.CreatedBy || Comment.MakeReadOnly)
                    $("#saveCommentButton").attr("disabled","disabled")
                $("#dialog").dialog({
                    modal: true,
                    position: "center",
                    width: 440,
                    resizable: false,
                    title:"View/Edit Comment",
                    //jqueryUI buttons
                    open: function (event, ui) {
                        //style dialog buttons
                        styleDialogInputs();
                    }
                });
            }
        });
    }
}