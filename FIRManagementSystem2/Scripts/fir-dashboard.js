// fir-dashboard.js – Manual FIR number, text IO input, status dropdown, filters

var statusOptions = [];   // holds status codes for the modal dropdown
var statusLabels = {      // display-friendly labels for badges
    'REGISTERED': 'Registered',
    'RESOLVED': 'Resolved',
    'UNDER_TRIAL': 'Under Trial',
    'DORMANT': 'Dormant'
};

function loadDropdowns() {
    // 1. Investigating Officers - ONLY for filter dropdown
    $.getJSON('/FIR/GetIOList', function (data) {
        var $filterIO = $('#filterIO');
        $filterIO.empty().append('<option value="">All Officers</option>');
        $.each(data, function (i, io) {
            $filterIO.append($('<option>', { value: io.name, text: io.name }));
        });
    });

    // 2. Document Types for upload modal
    $.getJSON('/FIR/GetDocTypes', function (data) {
        var $docType = $('#uploadDocType');
        $docType.empty().append('<option value="" disabled selected>Select type</option>');
        $.each(data, function (i, dt) {
            $docType.append($('<option>', { value: dt.value, text: dt.label }));
        });
    });

    // 3. FIR Statuses for modal and filter dropdowns
    $.getJSON('/FIR/GetStatusOptions', function (data) {
        statusOptions = data;
        // Modal status dropdown
        var $statusSelect = $('#newStatus');
        $statusSelect.empty();
        $.each(data, function (i, st) {
            $statusSelect.append($('<option>', { value: st.value, text: st.label }));
        });
        // Filter status dropdown
        var $filterStatus = $('#filterStatus');
        $filterStatus.empty().append('<option value="">All Statuses</option>');
        $.each(data, function (i, st) {
            $filterStatus.append($('<option>', { value: st.value, text: st.label }));
        });
    });
}
loadDropdowns();

// ── Helper functions ──
function fmtDate(iso) {
    if (!iso) return '';
    return new Date(iso + 'T00:00:00').toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

function badgeHtml(s) {
    var m = {
        'REGISTERED': 'badge-active',
        'RESOLVED': 'badge-closed',
        'UNDER_TRIAL': 'badge-investigation',
        'DORMANT': 'badge-pending'
    };
    var label = statusLabels[s] || s;   // use human-friendly name
    return '<span class="badge-pill ' + (m[s] || 'badge-closed') + '"><span class="dot"></span>' + label + '</span>';
}

function actHtml(firId) {
    return '<div class="actions">' +
        '<button class="act-btn view" title="View" data-id="' + firId + '"><i class="bi bi-eye"></i></button>' +
        '<button class="act-btn edit" title="Edit" data-id="' + firId + '"><i class="bi bi-pencil"></i></button>' +
        '<button class="act-btn del"  title="Delete" data-id="' + firId + '"><i class="bi bi-trash"></i></button>' +
        '<button class="act-btn attach" title="Attach File" data-id="' + firId + '"><i class="bi bi-paperclip"></i></button>' +
        '</div>';
}

// ── DataTable with AJAX source ──
var dt;
if ($('#firTable').length) {
    try {
        dt = $('#firTable').DataTable({
            ajax: {
                url: '/FIR/GetFIRList',
                dataSrc: function (json) {
                    // Main stat numbers
                    $('#st-total').text(json.stats.total);
                    $('#st-closed').text(json.stats.resolved);
                    $('#st-invest').text(json.stats.underTrial);
                    $('#st-pending').text(json.stats.dormant);
                    $('#sidebarFirCount').text(json.stats.total);
                    $('#recordCount').text(json.stats.total + ' case' + (json.stats.total !== 1 ? 's' : '') + ' registered');

                    function deltaHtml(current, previous, type) {
                        var diff = current - previous;
                        var abs = Math.abs(diff);
                        var label = '';

                        if (type === 'month') {
                            label = abs + ' this month';
                        } else if (type === 'day') {
                            label = abs + ' today';
                        }

                        if (diff > 0) {
                            return '<span class="stat-delta up"><i class="bi bi-arrow-up-short"></i>+' + label + '</span>';
                        } else if (diff < 0) {
                            return '<span class="stat-delta down"><i class="bi bi-arrow-down-short"></i>-' + label + '</span>';
                        } else {
                            return '<span class="stat-delta neutral"><i class="bi bi-dash"></i>No change</span>';
                        }
                    }

                    // Update delta spans with correct property names from the controller
                    $('#st-total-delta').html(deltaHtml(json.stats.totalThisMonth, json.stats.totalLastMonth, 'month'));
                    $('#st-closed-delta').html(deltaHtml(json.stats.resolvedThisMonth, json.stats.resolvedLastMonth, 'month'));
                    $('#st-invest-delta').html(deltaHtml(json.stats.underTrialToday, json.stats.underTrialYesterday, 'day'));
                    $('#st-pending-delta').html(deltaHtml(json.stats.dormantThisMonth, json.stats.dormantLastMonth, 'month'));

                    return json.data;
                }
            },
            pageLength: 10,
            order: [[1, 'desc']],
            autoWidth: false,
            dom: 't<"__dtfoot"ip>',
            columns: [
                {
                    data: null, orderable: false,
                    render: function (d, t, r, meta) {
                        return '<span class="row-sno">' + String(meta.row + 1).padStart(2, '0') + '</span>';
                    }
                },
                {
                    data: 'firNo',
                    render: function (data, type, row) {
                        return '<a class="fir-no" href="/FIR/Details/' + row.firId + '">' + data + '</a>';
                    }
                },
                { data: 'date', render: d => '<span style="color:var(--ink-muted);font-size:.79rem">' + fmtDate(d) + '</span>' },
                { data: 'desc', className: 'col-desc', render: (d, t) => t === 'display' ? '<span title="' + d.replace(/"/g, '&quot;') + '">' + d + '</span>' : d },
                { data: 'complainant', render: d => '<span style="font-weight:600">' + d + '</span>' },
                { data: 'accused', render: d => '<span style="color:var(--ink-muted);font-size:.79rem">' + d + '</span>' },
                { data: 'io', render: d => '<span style="font-size:.8rem">' + d + '</span>' },
                { data: 'status', render: d => badgeHtml(d) },
                { data: 'firId', orderable: false, render: d => actHtml(d) }
            ],
            drawCallback: function () {
                var i = 1;
                this.api().rows({ page: 'current' }).every(function () {
                    var sno = this.node().querySelector('.row-sno');
                    if (sno) sno.textContent = String(i++).padStart(2, '0');
                });
                var wrap = document.getElementById('firTable_wrapper');
                var inner = wrap ? wrap.querySelector('.__dtfoot') : null;
                if (inner) {
                    var infoEl = inner.querySelector('.dataTables_info');
                    var pagEl = inner.querySelector('.dataTables_paginate');
                    if (infoEl) document.getElementById('dtInfo').replaceWith(infoEl);
                    if (pagEl) document.getElementById('dtPaginate').replaceWith(pagEl);
                }
            }
        });

        // Filters
        $('#customSearch').on('keyup', function () { dt.search(this.value).draw(); });
        // ── Custom filter for Status (uses raw data, not rendered HTML) ──
        $('#filterStatus').on('change', function () {
            var val = this.value;
            // Remove all custom filters
            $.fn.dataTable.ext.search = [];

            if (val) {
                $.fn.dataTable.ext.search.push(
                    function (settings, data, dataIndex) {
                        // Get the raw row object via DataTable API
                        var rowData = dt.row(dataIndex).data();
                        return rowData && rowData.status === val;
                    }
                );
            }
            dt.draw();
        });
        $('#filterIO').on('change', function () { dt.column(6).search(this.value).draw(); });

    } catch (e) {
        console.error('DataTable init failed:', e);
        alert('Table initialisation error – check console.');
    }
}

// ── View FIR details ──
$('#firTable').on('click', '.act-btn.view', function () {
    var r = dt.row($(this).closest('tr')).data();
    if (!r) return;
    var html =
        '<div class="detail-row"><div class="detail-label">FIR Number</div><div class="detail-value" style="font-weight:700;color:var(--teal-mid)">' + r.firNo + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Date</div><div class="detail-value">' + fmtDate(r.date) + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Description</div><div class="detail-value" style="white-space:normal;line-height:1.6">' + r.desc + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Complainant</div><div class="detail-value">' + r.complainant + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Accused</div><div class="detail-value">' + r.accused + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Investigating Officer</div><div class="detail-value">' + r.io + '</div></div>' +
        '<div class="detail-row"><div class="detail-label">Status</div><div class="detail-value">' + badgeHtml(r.status) + '</div></div>' +
        '<div class="mt-3"><strong>Attachments</strong><div id="documentsList-' + r.firId + '">Loading...</div></div>';
    $('#viewBody').html(html);
    new bootstrap.Modal(document.getElementById('viewModal')).show();

    $.getJSON('/FIR/GetDocuments', { firId: r.firId }, function (data) {
        var c = $('#documentsList-' + r.firId);
        if (!data || !data.length) { c.html('<p class="text-muted">No attachments found.</p>'); return; }
        var lst = '<ul class="list-unstyled">';
        $.each(data, function (i, d) {
            lst += '<li class="mb-2"><i class="bi bi-file-earmark me-2"></i><a href="' + d.downloadUrl + '">' + d.fileName + '</a> ' +
                '<small class="text-muted">(' + d.docType + ', ' + (d.fileSizeKb || '?') + ' KB)</small>' +
                (d.description ? '<br/><small>' + d.description + '</small>' : '') + '</li>';
        });
        lst += '</ul>';
        c.html(lst);
    }).fail(function () {
        $('#documentsList-' + r.firId).html('<p class="text-danger">Failed to load attachments.</p>');
    });
});

// ── Edit FIR (IO field is now a text input) ──
$('#firTable').on('click', '.act-btn.edit', function () {
    var rowData = dt.row($(this).closest('tr')).data();
    if (!rowData) return;

    $('#firIdHidden').val(rowData.firId);
    $('#newFirNo').val(rowData.firNo);
    $('#newDate').val(rowData.date);
    $('#newDesc').val(rowData.desc);
    $('#newComplainant').val(rowData.complainant);
    $('#newAccused').val(rowData.accused);
    $('#newLocation').val(rowData.location || '');
    $('#newPoliceStation').val(rowData.policeStation || '');
    $('#newSections').val(rowData.sections || '');

    // IO is a text input now
    $('#newIO').val(rowData.io || '');

    // Status is a dropdown
    $('#newStatus').val(rowData.status);

    $('#modalTitle').html('<i class="bi bi-pencil-square"></i> Edit FIR');
    new bootstrap.Modal(document.getElementById('registerModal')).show();
});

// ── Delete FIR ──
$('#firTable').on('click', '.act-btn.del', function () {
    var id = $(this).data('id');
    var firNo = $(this).closest('tr').find('.fir-no').text();
    if (confirm('Delete ' + firNo + '?')) {
        var form = $('<form>', {
            method: 'post',
            action: '/FIR/Delete'
        }).append($('<input>', { type: 'hidden', name: 'id', value: id }));
        $('body').append(form);
        form.submit();
    }
});

// ── Attach / Upload (unchanged) ──
$('#firTable').on('click', '.act-btn.attach', function () {
    var id = $(this).data('id');
    $('#uploadFirId').val(id);
    $('#uploadFile').val('');
    $('#uploadDesc').val('');
    $('#uploadDocType').val('');
    new bootstrap.Modal(document.getElementById('uploadModal')).show();
});

$('#submitUpload').on('click', function () {
    var firId = $('#uploadFirId').val();
    var docType = $('#uploadDocType').val();
    var desc = $('#uploadDesc').val().trim();
    var fileInput = $('#uploadFile')[0];
    if (!firId || !docType || !fileInput.files.length) { alert('Please fill all required fields and select a file.'); return; }
    var formData = new FormData();
    formData.append('firId', firId);
    formData.append('docType', docType);
    formData.append('description', desc);
    formData.append('file', fileInput.files[0]);
    $.ajax({
        url: '/FIR/UploadDocument',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (resp) {
            if (resp.success) {
                showToast('Document uploaded successfully.');
                bootstrap.Modal.getInstance(document.getElementById('uploadModal')).hide();
            } else { alert('Error: ' + resp.message); }
        },
        error: function () { alert('Upload failed.'); }
    });
});

// ── Modal lifecycle ──
$('#registerModal').on('show.bs.modal', function () {
    // Only reset if it's a truly new FIR (no edit ID and no case link)
    if (!$('#firIdHidden').val() && !$('#firCaseIdHidden').val()) {
        $('#firForm')[0].reset();
        $('#newDate').val(new Date().toISOString().slice(0, 10));
        var firstStatus = $('#newStatus option:first').val();
        if (firstStatus) {
            $('#newStatus').val(firstStatus);
        }
        $('#modalTitle').html('<i class="bi bi-file-earmark-plus"></i> Add New FIR');
    }
});
$('#registerModal').on('hidden.bs.modal', function () {
    $('#firIdHidden').val('');
    $('#modalTitle').html('<i class="bi bi-file-earmark-plus"></i> Add New FIR');
});

// ── Export CSV ──
$('#exportBtn').on('click', function () {
    var data = dt.rows({ search: 'applied' }).data().toArray();
    var hdr = ['FIR No', 'Date', 'Description', 'Complainant', 'Accused', 'IO', 'Status'];
    var rows = data.map(r => [r.firNo, fmtDate(r.date), '"' + r.desc.replace(/"/g, '""') + '"', r.complainant, r.accused, r.io, r.status]);
    var csv = [hdr].concat(rows).map(r => r.join(',')).join('\n');
    var a = document.createElement('a');
    a.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv);
    a.download = 'FIR-Records-' + new Date().toISOString().slice(0, 10) + '.csv';
    a.click();
    showToast('CSV exported.');
});