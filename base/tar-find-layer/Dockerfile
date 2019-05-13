FROM lambci/lambda-base-2

# We don't have `find` on Lambda anymore, so use bash to simulate find <dir> ! -type d
RUN find() { local d; for d in "$@"; do ls -1A "$d" | while read f; do i="$d/$f"; [ -d "$i" ] && [ ! -L "$i" ] && find "$i" || echo $i; done; done; } && \
  cd /usr && \
  find bin lib64 | sort > /fs.txt


FROM lambci/lambda-base-2:build

COPY --from=0 /fs.txt /

RUN mkdir -p /tmp/etc && \
  cp /etc/yum.conf /tmp/etc/ && \
  cp -R /etc/yum /tmp/etc/ && \
  echo 2 > /tmp/etc/yum/vars/releasever && \
  yum install -y --installroot=/tmp findutils gzip tar && \
  cd /tmp/usr && \
  bash -c 'comm -13 /fs.txt <(find bin lib64 ! -type d | sort)' | tar -c -T - | tar -x -C /opt && \
  cd /opt && \
  rm -rf lib && \
  mv lib64 lib && \
  zip -yr /tmp/layer.zip bin lib

# docker build -t tar-ps-layer .
# docker run --rm tar-ps-layer cat /tmp/layer.zip > layer.zip
