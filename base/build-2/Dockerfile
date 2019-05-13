FROM amazonlinux:2

RUN mkdir -p /tmp/etc && \
  cp /etc/yum.conf /tmp/etc/ && \
  cp -R /etc/yum /tmp/etc/ && \
  echo 2 > /tmp/etc/yum/vars/releasever && \
  yum install -y --installroot=/tmp yum yum-plugin-ovl yum-plugin-priorities


FROM lambci/lambda-base-2

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

COPY --from=0 /tmp /

RUN yum install -y glibc-langpack-en && \
  yum groupinstall -y development && \
  yum install -y which clang cmake python-devel python3-devel amazon-linux-extras && \
  amazon-linux-extras install -y docker && \
  pip3 install -U pip setuptools --no-cache-dir && \
  yum clean all && \
  rm -rf /var/cache/yum
