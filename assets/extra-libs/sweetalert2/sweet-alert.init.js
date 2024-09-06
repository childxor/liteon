// lang คือตัวแปรภาษาที่ดึงจาก DB ดูใน index.php และ language_system() ใน efs_lib.php
!function ($) {
    "use strict";

    var SweetAlert = function () { };

    //examples 
    SweetAlert.prototype.init = function () {

        //Logout Message
        $('.logout').click(function () {
            let href = $(this).data('href');
            Swal.fire({
                title: lang.sys_js_sweet2_logout_txt,
                text: "",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#3085d6",
                confirmButtonText: lang.sys_js_yes,
                cancelButtonColor: "#d33",
                cancelButtonText: lang.sys_js_no,
                closeOnConfirm: false
            }).then((result) => {
                if (result.value) {
                    $('.auto-save').savy('destroy', function () {
                        console.log("All data from savy are destroyed");
                    });
                    $(location).attr('href', href);
                }
            });
        });

        //Confirm Delete Message
        $('#mTable tbody').on('click', '.confirmDelete', function () {
            let href = $(this).data('href');
            Swal.fire({
                title: lang.sys_js_sweet2_confirm,
                text: lang.sys_js_confirm_delete,
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#3085d6",
                confirmButtonText: lang.sys_js_yes,
                cancelButtonColor: "#d33",
                cancelButtonText: lang.sys_js_no,
                closeOnConfirm: false
            }).then((result) => {
                if (result.value) {
                    $(location).attr('href', href);
                }
            });
        });

        //Confirm Select Delete Message
        $('.confirmSelectDelete').on('click', function () {
            var chk = "";
            $('input[name="chkDelete[]"]:checkbox:checked').each(function (i) {
                chk += '|' + $(this).val();
            });
            let href = $(this).data('href');

            Swal.fire({
                title: "ยืนยันการทำรายการ",
                text: "คุณต้องการลบรายการที่เลือกนี้หรือไม่!",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#3085d6",
                confirmButtonText: "ใช่, ลบเลย!",
                cancelButtonColor: "#d33",
                cancelButtonText: "ไม่",
                closeOnConfirm: false
            }).then((result) => {
                if (result.value) {
                    $(location).attr('href', href + chk);
                }
            });
        });

        $("#sa-confirm").click(function () {
            Swal.fire({
                title: 'Are you sure?',
                text: "You won't be able to revert this!",
                type: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            }).then((result) => {
                if (result.value) {
                    Swal.fire(
                        'Deleted!',
                        'Your file has been deleted.',
                        'success'
                    )
                }
            })
        });

        $("#sa-passparameter").click(function () {
            const swalWithBootstrapButtons = Swal.mixin({
                customClass: {
                    confirmButton: 'btn btn-success',
                    cancelButton: 'mr-2 btn btn-danger'
                },
                buttonsStyling: false,
            })

            swalWithBootstrapButtons.fire({
                title: 'Are you sure?',
                text: "You won't be able to revert this!",
                type: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes, delete it!',
                cancelButtonText: 'No, cancel!',
                reverseButtons: true
            }).then((result) => {
                if (result.value) {
                    swalWithBootstrapButtons.fire(
                        'Deleted!',
                        'Your file has been deleted.',
                        'success'
                    )
                } else if (
                    // Read more about handling dismissals
                    result.dismiss === Swal.DismissReason.cancel
                ) {
                    swalWithBootstrapButtons.fire(
                        'Cancelled',
                        'Your imaginary file is safe :)',
                        'error'
                    )
                }
            })
        });

        $("#sa-bg").click(function () {
            Swal.fire({
                title: 'Custom width, padding, background.',
                width: 600,
                padding: '3em',
                background: '#fff url(../assets/images/background/active-bg.png)',
                backdrop: `
                        rgba(0,0,123,0.4)
                        url("../assets/images/background/nyan-cat.gif")
                        center left
                        no-repeat
                    `
            })
        });

        $("#sa-autoclose").click(function () {
            let timerInterval
            Swal.fire({
                title: 'Auto close alert!',
                html: 'I will close in <strong></strong> seconds.',
                timer: 2000,
                onBeforeOpen: () => {
                    Swal.showLoading()
                    timerInterval = setInterval(() => {
                        Swal.getContent().querySelector('strong')
                            .textContent = Swal.getTimerLeft()
                    }, 100)
                },
                onClose: () => {
                    clearInterval(timerInterval)
                }
            }).then((result) => {
                if (
                    // Read more about handling dismissals
                    result.dismiss === Swal.DismissReason.timer
                ) {
                    console.log('I was closed by the timer')
                }
            })
        });

        $("#sa-rtl").click(function () {
            Swal.fire({
                title: 'هل تريد الاستمرار؟',
                type: 'question',
                customClass: {
                    icon: 'swal2-arabic-question-mark'
                },
                confirmButtonText: 'نعم',
                cancelButtonText: 'لا',
                showCancelButton: true,
                showCloseButton: true
            })
        });

        $("#sa-ajax").click(function () {
            Swal.fire({
                title: 'Submit your Github username',
                input: 'text',
                inputAttributes: {
                    autocapitalize: 'off'
                },
                showCancelButton: true,
                confirmButtonText: 'Look up',
                showLoaderOnConfirm: true,
                preConfirm: (login) => {
                    return fetch(`//api.github.com/users/${login}`)
                        .then(response => {
                            if (!response.ok) {
                                throw new Error(response.statusText)
                            }
                            return response.json()
                        })
                        .catch(error => {
                            Swal.showValidationMessage(
                                `Request failed: ${error}`
                            )
                        })
                },
                allowOutsideClick: () => !Swal.isLoading()
            }).then((result) => {
                if (result.value) {
                    Swal.fire({
                        title: `${result.value.login}'s avatar`,
                        imageUrl: result.value.avatar_url
                    })
                }
            })
        });

        $("#sa-chain").click(function () {
            Swal.mixin({
                input: 'text',
                confirmButtonText: 'Next &rarr;',
                showCancelButton: true,
                progressSteps: ['1', '2', '3']
            }).queue([
                {
                    title: 'Question 1',
                    text: 'Chaining swal2 modals is easy'
                },
                'Question 2',
                'Question 3'
            ]).then((result) => {
                if (result.value) {
                    Swal.fire({
                        title: 'All done!',
                        html:
                            'Your answers: <pre><code>' +
                            JSON.stringify(result.value) +
                            '</code></pre>',
                        confirmButtonText: 'Lovely!'
                    })
                }
            })
        });

        $("#sa-queue").click(function () {
            const ipAPI = 'https://api.ipify.org?format=json'

            Swal.queue([{
                title: 'Your public IP',
                confirmButtonText: 'Show my public IP',
                text:
                    'Your public IP will be received ' +
                    'via AJAX request',
                showLoaderOnConfirm: true,
                preConfirm: () => {
                    return fetch(ipAPI)
                        .then(response => response.json())
                        .then(data => Swal.insertQueueStep(data.ip))
                        .catch(() => {
                            Swal.insertQueueStep({
                                type: 'error',
                                title: 'Unable to get your public IP'
                            })
                        })
                }
            }])
        });

        $("#sa-timerfun").click(function () {
            let timerInterval
            Swal.fire({
                title: 'Auto close alert!',
                html:
                    'I will close in <strong></strong> seconds.<br/><br/>' +
                    '<button id="increase" class="btn btn-warning">' +
                    'I need 5 more seconds!' +
                    '</button><br/>' +
                    '<button id="stop" class="btn btn-danger mt-1">' +
                    'Please stop the timer!!' +
                    '</button><br/>' +
                    '<button id="resume" class="btn btn-success mt-1" disabled>' +
                    'Phew... you can restart now!' +
                    '</button><br/>' +
                    '<button id="toggle" class="btn btn-primary mt-1">' +
                    'Toggle' +
                    '</button>',
                timer: 10000,
                onBeforeOpen: () => {
                    const content = Swal.getContent()
                    const $ = content.querySelector.bind(content)

                    const stop = $('#stop')
                    const resume = $('#resume')
                    const toggle = $('#toggle')
                    const increase = $('#increase')

                    Swal.showLoading()

                    function toggleButtons() {
                        stop.disabled = !Swal.isTimerRunning()
                        resume.disabled = Swal.isTimerRunning()
                    }

                    stop.addEventListener('click', () => {
                        Swal.stopTimer()
                        toggleButtons()
                    })

                    resume.addEventListener('click', () => {
                        Swal.resumeTimer()
                        toggleButtons()
                    })

                    toggle.addEventListener('click', () => {
                        Swal.toggleTimer()
                        toggleButtons()
                    })

                    increase.addEventListener('click', () => {
                        Swal.increaseTimer(5000)
                    })

                    timerInterval = setInterval(() => {
                        Swal.getContent().querySelector('strong')
                            .textContent = (Swal.getTimerLeft() / 1000)
                                .toFixed(0)
                    }, 100)
                },
                onClose: () => {
                    clearInterval(timerInterval)
                }
            })
        });
    },
        //init
        $.SweetAlert = new SweetAlert, $.SweetAlert.Constructor = SweetAlert
}(window.jQuery),
    //initializing 
    function ($) {
        "use strict";
        $.SweetAlert.init()
    }(window.jQuery);